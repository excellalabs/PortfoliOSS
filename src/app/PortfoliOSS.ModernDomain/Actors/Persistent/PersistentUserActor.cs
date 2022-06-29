using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.Messages;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent
{
    public class PersistentUserActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly string _userName;
        private readonly int _userId;
        private UserState _state;
        private readonly ActorSelection _githubActor;
        private ActorSelection _orgManager;
        public PersistentUserActor(string userName, int userId)
        {
            _logger = Context.GetLogger();
            _userName = userName;
            _userId = userId;
            _state = new UserState();
            _githubActor = Context.ActorSelection(Constants.ActorPaths.GITHUB_CLIENT);
            _orgManager = Context.ActorSelection(Constants.ActorPaths.ORG_MANAGER);

            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is UserState state)
                {
                    _state = state;
                }
            });

            Recover<RecoveryCompleted>(msg =>
            {
                _logger.Info("RECOVERY: Completed for User '{UserName}'. Part of {OrgCount} orgs, MemberOfOrg is {MemberOfOrg}, {ForkCount} forks, {SourceCount} sources", _userName, _state.MemberOrgs.Count, _state.IsPartOfOrg, _state.Forks.Count, _state.Sources.Count);

                if (!_state.IsPartOfOrg)
                {
                    _orgManager.Tell(new IsUserPartOfAnOrg(_userId));
                }
                else
                {
                    GetForksAndSources();
                }
            });

            Recover<OrgAddedForUserEvent>(Apply);
            Recover<ForkAddedForUserEvent>(Apply);
            Recover<SourceAddedForUserEvent>(Apply);
            Recover<PrAddedEvent>(Apply);
            Command<ForkListForUser>(msg =>
            {
                _logger.Info("Processing {ForkCount} forks for user {UserName}", msg.Forks.Count, msg.UserName);
                foreach (var fork in msg.Forks)
                {
                    Self.Tell(new AddForkForUserCommand(fork.RepoFullName, fork.RepoId, fork.OrganizationName, fork.Name, fork.OrgId));
                }
            });

            Command<SourceListForUser>(msg =>
            {
                _logger.Info("Processing {SourceCount} sources for user {UserName}", msg.Sources.Count, msg.UserName);
                foreach (var fork in msg.Sources)
                {
                    Self.Tell(new AddSourceForUserCommand(fork.RepoFullName, fork.RepoId, fork.OrganizationName, fork.Name, fork.OrgId));
                }
            });


            Command<UserIsPartOfOrg>(msg =>
            {
                Self.Tell(new AddOrgForUserCommand(msg.OrgName, msg.OrgId, msg.UserName, msg.UserId));
            });

            Command<PRInfoList>(msg =>
            {
                foreach (var pr in msg.PrInfoList)
                {
                    Self.Tell(new AddPRCommand(pr.RepoFullName, pr.RepoId, pr.PullRequestId, pr.AuthorLogin, pr.IsMerged, pr.CreatedDate, pr.PRMergeDate, pr.AuthorId));
                }
            });

            Command<AddPRCommand>(cmd =>
            {
                if (cmd.AuthorUserId != _userId)
                {
                    _logger.Warning("AddPRCommand: User {CmdUserName} doesn't match my Username {UserName}", cmd.AuthorUserId, _userId);
                    return;
                }

                if (_state.PullRequests.Any(x => x.PullRequestId == cmd.PullRequestId && x.RepoId == cmd.RepoId))
                {
                    _logger.Info("AddPRCommand: PR ID {PRId} for Repo {RepoId} already in state for user {UserName}", cmd.PullRequestId, cmd.RepoId, _userName);
                    return;
                }

                _logger.Info("AddPRCommand: Persisting Event -- PR {PRId} for {RepoName} for {UserName}",cmd.PullRequestId, cmd.RepoFullName, _userName);
                var @event = new PrAddedEvent(cmd.RepoFullName, cmd.RepoId, cmd.PullRequestId, cmd.AuthorLogin, cmd.AuthorUserId, cmd.isMerged, cmd.createdDate, cmd.mergedDate);
                Persist(@event, Apply);
            });


            Command<AddForkForUserCommand>(cmd =>
            {
                if (_state.Forks.All(x => x.RepoId != cmd.RepoId))
                {
                    _logger.Info("Persisting ForkAddedForUserEvent for '{RepoFullName}'", cmd.RepoFullName);
                    Persist(new ForkAddedForUserEvent(cmd.RepoFullName, cmd.RepoId, cmd.OrganizationName, cmd.RepoName, cmd.OrgId), Apply);
                }
            });

            Command<AddSourceForUserCommand>(cmd =>
            {
                if (_state.Sources.All(x => x.RepoId != cmd.RepoId))
                {
                    _logger.Info("Persisting SourceAddedForUserEvent for '{RepoFullName}'", cmd.RepoFullName);
                    Persist(new SourceAddedForUserEvent(cmd.RepoFullName, cmd.RepoId, cmd.OrganizationName, cmd.RepoName, cmd.OrgId), Apply);
                }
            });

            Command<AddOrgForUserCommand>(cmd =>
            {
                _logger.Info("Processing AddOrgForUserCommand: {OrgName} for User", cmd.OrgName);
                // TODO: Use ID not name -- but need to have it discovered (since it's not available when we create the org actor)
                if (!_state.MemberOrgs.Contains(cmd.OrgName, StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.Info("AddOrgForUserCommand: Persisting Event to {OrgName} for User", cmd.OrgName);
                    Persist(new OrgAddedForUserEvent(cmd.OrgName, cmd.OrgId, cmd.UserName, cmd.UserId), Apply);
                    GetForksAndSources();
                }
            });
        }

        private void GetForksAndSources()
        {
            _logger.Info("GetForksAndSources called");
            _githubActor.Tell(new GetForksAndSourcesForUser(_userName));
        }

        public void Apply(object @event)
        {
            switch (@event)
            {
                case OrgAddedForUserEvent evt: 
                    _state.MemberOrgs.Add(evt.OrgName); // TODO: Change to memberorg type with name and ID
                    _state.IsPartOfOrg = true;
                    break;
                case ForkAddedForUserEvent evt:
                    _state.Forks.Add(new GitHubRepo(evt.RepoFullName, evt.RepoId, evt.OrganizationName, evt.RepoName, evt.OrgId));
                    break;
                case SourceAddedForUserEvent evt:
                    _state.Sources.Add(new GitHubRepo(evt.RepoFullName, evt.RepoId, evt.OrganizationName, evt.RepoName, evt.OrgId));
                    break;
                case PrAddedEvent evt:
                    var gitHubPr = new GitHubPR(evt.RepoFullName, evt.RepoId, evt.PullRequestId, evt.AuthorLogin,
                        evt.isMerged, evt.createdDate, evt.mergedDate);
                    _state.PullRequests.Add(gitHubPr);
                    break;

            }
        }


        /// <summary>
        /// Id of the persistent entity for which messages should be replayed.
        /// </summary>
        public override string PersistenceId => $"user-{_userId}";
    }
}
