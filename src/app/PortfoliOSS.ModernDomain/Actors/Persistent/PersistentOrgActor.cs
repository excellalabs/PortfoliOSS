using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.Messages;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent
{
    public class PersistentOrganization : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly string _orgName;
        private readonly int _orgId;
        private OrganizationState _state;
        private ActorSelection _githubActor;
        public PersistentOrganization(string orgName, int orgId)
        {
            _logger = Context.GetLogger();
            _orgName = orgName;
            _orgId = orgId;
            _state = new OrganizationState();
            _githubActor = Context.ActorSelection(Constants.ActorPaths.GITHUB_CLIENT);

            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is OrganizationState state)
                {
                    _state = state;
                }
            });

            Recover<UserAddedEvent>(Apply);

            Recover<RecoveryCompleted>(msg =>
            {
                _logger.Info("RECOVERY: Completed for Org {OrgName}; {UserCount} users", _orgName, _state.Users.Count);
                _githubActor.Tell(new GetMembersOfGithubOrg(_orgName));
                _githubActor.Tell(new GetForksAndSourcesForOrg(_orgName, _orgId));
            });

            Command<IsUserPartOfAnOrg>(msg =>
            {
                var user = _state.Users.FirstOrDefault(x => x.UserId == msg.UserId);
                if (user != null)
                {
                    Sender.Tell(new UserIsPartOfOrg(msg.UserId, _orgName, _orgId, user.Username));
                }
            });

            Command<UsersDiscovered>(msg =>
            {
                foreach (var user in msg.Users)
                {
                    Self.Tell(new AddUserCommand(user.Username, user.UserId));
                }
            });

            Command<AddUserCommand>(cmd =>
            {
                if (_state.Users.All(x => x.UserId != cmd.UserId))
                {
                    Persist(new UserAddedEvent(cmd.Username, cmd.UserId), Apply);
                }
            });
        }

        public void Apply(object @event)
        {
            switch (@event)
            {
                case UserAddedEvent evt:
                    _state.Users.Add(new GitHubUser(evt.Username, evt.UserId));
                    break;
            }
        }


        /// <summary>
        /// Id of the persistent entity for which messages should be replayed.
        /// </summary>
        public override string PersistenceId => $"org-{_orgName}";
    }
}
