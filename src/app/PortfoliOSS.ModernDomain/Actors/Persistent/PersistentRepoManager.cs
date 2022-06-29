using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent;

public class PersistentRepoManager : ReceivePersistentActor
{
    private ILoggingAdapter _logger;
    private RepoManagerState _state;
    private ActorSelection _githubActor;
    public PersistentRepoManager()
    {
        _logger = Context.GetLogger();
        _state = new RepoManagerState();
        _githubActor = Context.ActorSelection(Constants.ActorPaths.GITHUB_CLIENT);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is RepoManagerState state)
            {
                _state = state;
            }
        });

        Recover<ForkRepoAddedEvent>(Apply);
        Recover<SourceRepoAddedEvent>(Apply);
        Recover<SourceAddedForForkEvent>(Apply);

        Recover<RecoveryCompleted>(msg =>
        {
            _logger.Info("RECOVERY: Completed for RepoManager; {ForkCount} forks, {SourceCount} sources", _state.Forks.Count, _state.Sources.Count);
            var forksWithoutSources = _state.Forks.Where(x => !x.SourceRepoId.HasValue).ToList();
            _logger.Info("RepoManager: {ForksWithoutSourcesCount} forks without sources", forksWithoutSources.Count);
            foreach (var fork in forksWithoutSources)
            {
                _githubActor.Tell(new GetSourceForFork(fork.Repo));
            }
        });

        Command<ForkListForUser>(msg =>
        {
            _logger.Info("Receiving {ForkCount} forks to process for user {UserName}", msg.Forks.Count, msg.UserName);
            foreach (var fork in msg.Forks)
            {
                Self.Tell(new AddForkRepoCommand(fork.RepoFullName, fork.RepoId, fork.OrganizationName, fork.Name, fork.OrgId));
            }
        });

        Command<SourceListForUser>(msg =>
        {
            _logger.Info("Receiving {SourceCount} sources to process for user {UserName}", msg.Sources.Count, msg.UserName);
            foreach (var fork in msg.Sources)
            {
                Self.Tell(new AddSourceRepoCommand(fork.RepoFullName, fork.RepoId, fork.OrganizationName, fork.Name, fork.OrgId));
            }
        });

        Command<PRInfoList>(msg =>
        {
            _logger.Info("Receiving {PRCount} PR items for Repo {RepoName}", msg.PrInfoList.Count, msg.RepoName);
            try
            {
                var children = Context.GetChildren();
                var childPathNames = children.Select(x => x.Path.Name);
                var repoActorChild = children.Single(x => x.Path.Name == msg.RepoName.Replace('/', '_'));
                repoActorChild.Forward(msg);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error passing {PRCount} PRs for Repo {RepoName}", msg.PrInfoList.Count, msg.RepoName);
            }
        });

        Command<AddForkRepoCommand>(cmd =>
        {
            _logger.Info("Processing AddForkRepo Command for {ForkRepoFullName}", cmd.RepoFullName);
            if (_state.Forks.All(x => x.Repo.RepoId != cmd.RepoId))
            {
                _logger.Info("Persisting ForkRepoAddedEvent for {ForkRepoFullName}", cmd.RepoFullName);
                Persist(new ForkRepoAddedEvent(cmd.RepoFullName, cmd.RepoId, cmd.OrganizationName, cmd.RepoName, cmd.OrgId), Apply);
                 _logger.Info("Fork {ForkRepoFullName} is new, so we're also going to get fork from state ask for its source", cmd.RepoFullName);
                 _githubActor.Tell(new GetSourceForFork(new GitHubRepo(cmd.RepoFullName,cmd.RepoId,cmd.OrganizationName, cmd.RepoName, cmd.OrgId)));
            }
            else
            {
                _logger.Info("Fork {ForkRepoFullName} already exists in state; checking to see if it has a source", cmd.RepoFullName);
                var fork = _state.Forks.Single(x => x.Repo.RepoId == cmd.RepoId);
                if (!fork.SourceRepoId.HasValue)
                {
                    _logger.Info("Fork {ForkRepoFullName} does not have a source associated; asking for it", cmd.RepoFullName);
                    _githubActor.Tell(new GetSourceForFork(fork.Repo));
                }
                else
                {
                    _logger.Info("Fork {ForkRepoFullName} already has source associated with it", cmd.RepoFullName);
                }
            }
        });

        Command<AddSourceRepoCommand>(cmd =>
        {
            _logger.Info("Processing AddSourceRepo Command for {SourceRepoFullName}", cmd.RepoFullName);
            if (_state.Sources.All(x => x.RepoId != cmd.RepoId))
            {
                _logger.Info("Persisting SourceRepoAddedEvent for {SourceRepoFullName}", cmd.RepoFullName);
                Persist(new SourceRepoAddedEvent(cmd.RepoFullName, cmd.RepoId, cmd.OrganizationName, cmd.RepoName, cmd.OrgId), Apply);
            }
        });

        Command<AddSourceForForkCommand>(cmd =>
        {
            var fork = _state.Forks.SingleOrDefault(x => x.Repo.RepoId == cmd.ForkRepoId);
            if (fork != null && fork.SourceRepoId == null)
            {
                Persist(new SourceAddedForForkEvent(cmd.ForkRepoId, cmd.SourceRepoId), Apply);
            }
        });
    }

    public void Apply(object @event)
    {
        switch (@event)
        {
            case ForkRepoAddedEvent evt:
                var gitHubRepoAndSource = new GitHubRepoAndSource(new GitHubRepo(evt.RepoFullName, evt.RepoId, evt.OrganizationName, evt.RepoName, evt.OrgId),null);
                _state.Forks.Add(gitHubRepoAndSource);
                CreateRepoActorIfNotExists(evt.RepoId, evt.RepoFullName, evt.RepoFullName.Replace("/", "_"));
                break;
            case SourceRepoAddedEvent evt:
                _state.Sources.Add(new GitHubRepo(evt.RepoFullName, evt.RepoId, evt.OrganizationName, evt.RepoName, evt.OrgId));
                CreateRepoActorIfNotExists(evt.RepoId, evt.RepoFullName, evt.RepoFullName.Replace("/", "_"));
                break;
            case SourceAddedForForkEvent evt:
                var fork = _state.Forks.Single(x => x.Repo.RepoId == evt.ForkRepoId);
                fork = fork with { SourceRepoId = evt.SourceRepoId };
                break;
        }
    }

    private void CreateRepoActorIfNotExists(long evtRepoId, string evtRepoFullName, string actorName)
    {
        try
        {
            Context.ActorOf(Props.Create<PersistentRepoActor>(evtRepoFullName, evtRepoId), actorName);

        }
        catch (Exception ex)
        {
            if (ex is not InvalidActorNameException)
            {
                _logger.Warning(ex, "Not creating actor '{ActorName}' because we hit an exception. Repo full name '{RepoFullName}', Repo ID '{RepoId}'", actorName, evtRepoFullName, evtRepoId);
                throw;
            }
        }
    }

    /// <summary>
    /// Id of the persistent entity for which messages should be replayed.
    /// </summary>
    public override string PersistenceId => "RepoManager";
}