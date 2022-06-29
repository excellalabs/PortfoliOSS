using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent
{
    public class PersistentUserManager : ReceivePersistentActor
    {
        private ILoggingAdapter _logger;
        private UserManagerState _state;
        public PersistentUserManager()
        {
            _logger = Context.GetLogger();
            _state = new UserManagerState();

            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is UserManagerState state)
                {
                    _state = state;
                }
            });

            Recover<UserAddedEvent>(Apply);

            Recover<RecoveryCompleted>(msg =>
            {
                _logger.Info("RECOVERY: Completed for UserManager; {UserCount} users", _state.Users.Count);
            });

            Command<UsersDiscovered>(thing =>
            {
                foreach (var user in thing.Users)
                {
                    Self.Tell(new AddUserCommand(user.Username, user.UserId));
                }
            });
            Command<UsersDiscovered>(msg =>
            {
                foreach (var user in msg.Users)
                {
                    Self.Tell(new AddUserCommand(user.Username, user.UserId));
                }
            });

            Command<PRInfoList>(msg =>
            {
                _logger.Info("Receiving {PRCount} PR items for Repo {RepoName}", msg.PrInfoList.Count, msg.RepoName);
                var lookup = msg.PrInfoList.ToLookup(x => x.AuthorLogin);
                foreach (var userPRList in lookup)
                {
                    var user = userPRList.Key;
                    var prInfoListForUser = userPRList.ToList();
                    var userId = userPRList.First().AuthorId;
                    _logger.Info("Sending {PRCount} PRs to user {UserName}", prInfoListForUser.Count, user);
                    try
                    {
                        if (user.Contains("[bot]", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _logger.Info("User {UserName} appears to be a bot so we're skipping it", user);
                            continue;
                        }
                        else
                        {
                            var userActorChild = Context.GetChildren().Single(x => x.Path.Name == user);
                            userActorChild.Tell(new PRInfoList(prInfoListForUser, msg.RepoName));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Error occurred while attempting to find child actor for {UserName}. Attempting to create one.", user);
                        Self.Tell(new AddUserCommand(user, userId));
                        Self.Tell(prInfoListForUser);
                    }
                }
            });


            Command<AddUserCommand>(cmd =>
            {
                if (cmd.Username.Contains("[Bot]", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.Info("{UserName} appears to be a bot; skipping", cmd.Username);
                    return;
                }
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
                    Context.ActorOf(Props.Create<PersistentUserActor>(evt.Username, evt.UserId), evt.Username);
                    break;
            }
        }

        /// <summary>
        /// Id of the persistent entity for which messages should be replayed.
        /// </summary>
        public override string PersistenceId => "UserManager";
    }
}

