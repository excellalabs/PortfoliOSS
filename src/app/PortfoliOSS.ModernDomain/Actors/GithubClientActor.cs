using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace PortfoliOSS.ModernDomain.Actors
{
    public class DonatedGitHubToken
    {
        public string Person { get; set; }
        public string Token { get; set; }
    }

    public class GithubClientActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger;
        private readonly IActorRef _githubWorkerActor;
        private int _numberOfGithubRequestsMade;

        // TODO: This feels hacky as hell
        private List<string> _sourcesAdded = new List<string>();
        private List<string> _forksAdded = new List<string>();
        private List<string> _forksCheckedForSources = new List<string>();
        private List<string> _usersCheckedForForksAndSources = new List<string>();
        private List<DonatedGitHubToken> _tokens;

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 15,
                withinTimeRange: TimeSpan.FromMinutes(1),
                localOnlyDecider: ex =>
                {
                    switch (ex)
                    {
                        case Octokit.NotFoundException:
                            return Directive.Resume;
                        case ApiException when ex.Message == "Server Error":
                            return Directive.Resume;
                        default:
                            return Directive.Restart;
                    }
                });
        }

        public GithubClientActor(List<DonatedGitHubToken> tokens)
        {
            _tokens = tokens;
            _logger = Context.GetLogger();

            var actorsList = tokens.Select(pt => Context.ActorOf(Propmaster.GithubWorkerActor(pt.Token), pt.Person)).Select(x=>x.Path.ToString()).ToList();
            _githubWorkerActor = Context.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup(actorsList)), "githubWorker");
            
            Receive<LogRequestsMadeSoFar>(m =>
            {
                _logger.Info("So far, we've queued {NumberOfGithubRequests} requests to the Github API", _numberOfGithubRequestsMade);
            });

            Receive<GetMembersOfGithubOrg>(m =>
            {
                _logger.Info("GetMembersOfGithubOrg called by {SenderAddress}", Sender.Path);
                _numberOfGithubRequestsMade++;
                _githubWorkerActor.Forward(m);
            });

            Receive<GetForksAndSourcesForUser>(m =>
            {
                if (_usersCheckedForForksAndSources.Contains(m.UserName))
                {
                    _logger.Warning("{UserName} already has been checked for forks and sources", m.UserName);
                    return;
                }
                _logger.Info("GetForksAndSourcesForUser called by {SenderAddress}", Sender.Path);
                _numberOfGithubRequestsMade++;
                _usersCheckedForForksAndSources.Add(m.UserName);
                _githubWorkerActor.Forward(m);
            });
            Receive<GetForksAndSourcesForOrg>(m =>
            {
                _logger.Info("GetForksAndSourcesForOrg called by {SenderAddress}", Sender.Path);
                _numberOfGithubRequestsMade++;
                _githubWorkerActor.Forward(m);
            });

            Receive<GetPagedRepoPullRequests>(m =>
            {
                _logger.Info("GetPagedRepoPullRequests called by {SenderAddress}", Sender.Path);
                _numberOfGithubRequestsMade++;
                _githubWorkerActor.Forward(m);
            });

            Receive<GetSourceForFork>(m =>
            {
                if (_forksCheckedForSources.Contains(m.Fork.RepoFullName))
                {
                    _logger.Warning("{RepoFullName} already has been checked for source", m.Fork.RepoFullName);
                    return;
                }
                _logger.Info("GetSourceForFork called by {SenderAddress}", Sender.Path);
                _numberOfGithubRequestsMade++;
                _forksCheckedForSources.Add(m.Fork.RepoFullName);
                _githubWorkerActor.Forward(m);
            });

            Receive<AddOrgRequest>(msg =>
            {
                _numberOfGithubRequestsMade++;
                _githubWorkerActor.Forward(msg);
            });

            _logger.Info("GithubClientActor created and sitting at {ClientActorPath}", Self.Path);
            _logger.Info("Scheduling messages every 5 seconds to tell us how many requests we've made so far.", Self.Path);
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), Self, new LogRequestsMadeSoFar(), Self);
        }
    }
}