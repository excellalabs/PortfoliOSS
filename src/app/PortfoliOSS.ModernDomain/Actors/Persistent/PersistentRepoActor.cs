using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Google.Protobuf;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent;

public class PersistentRepoActor : ReceivePersistentActor
{
    private readonly ILoggingAdapter _logger;
    private readonly string _repoName;
    private readonly long _repoId;
    private RepoState _state;
    private ActorSelection _githubActor;
    public PersistentRepoActor(string repoName, long repoId)
    {
        _logger = Context.GetLogger();
        _repoName = repoName;
        _repoId = repoId;
        _state = new RepoState();
        _githubActor = Context.ActorSelection(Constants.ActorPaths.GITHUB_CLIENT);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is RepoState state)
            {
                _state = state;
            }
        });

        Recover<RecoveryCompleted>(msg =>
        {
            _logger.Info("RECOVERY: Completed for Repo {RepoFullName}; {PRCount} PRs", _repoName, _state.PullRequests.Count);
            var pageNum = _state.PullRequests.Count / Constants.PR_PAGE_SIZE;
            if(pageNum < 1){ pageNum = 1; }

            var latestPr = 0;

            if (_state.PullRequests.Any())
            {
                latestPr = _state.PullRequests.Max(x => x.PullRequestId);
            }
            _githubActor.Tell(new GetPagedRepoPullRequests(_repoName, _repoId, pageNum, latestPr));
        });

        Recover<PrAddedEvent>(Apply);

        Command<PRInfoList>(msg =>
        {
            foreach (var pr in msg.PrInfoList)
            {
                Self.Tell(new AddPRCommand(pr.RepoFullName, pr.RepoId, pr.PullRequestId, pr.AuthorLogin, pr.IsMerged, pr.CreatedDate, pr.PRMergeDate,pr.AuthorId));
            }
        });

        Command<AddPRCommand>(cmd =>
        {
            if (cmd.RepoId != _repoId)
            {
                _logger.Warning("AddPRCommand: Repo {CmdRepo} doesn't match my Repo {RepoName}", cmd.RepoFullName, _repoName);
                return;
            }

            if (_state.PullRequests.Any(x => x.PullRequestId == cmd.PullRequestId))
            {
                _logger.Info("AddPRCommand: PR ID {PRId} already in state for repo {RepoName}", cmd.PullRequestId, _repoName);
                return;
            }

            _logger.Info("AddPRCommand: Persisting Event -- PR {PRId} for {RepoName}", cmd.PullRequestId, cmd.RepoFullName);
            var @event = new PrAddedEvent(cmd.RepoFullName, cmd.RepoId, cmd.PullRequestId, cmd.AuthorLogin, cmd.AuthorUserId, cmd.isMerged, cmd.createdDate, cmd.mergedDate);
            Persist(@event,Apply);
        });
    }

    public void Apply(object @event)
    {
        switch (@event)
        {
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
    public override string PersistenceId => $"repo-{_repoName}"; // TODO: Use Repo ID if possible
}