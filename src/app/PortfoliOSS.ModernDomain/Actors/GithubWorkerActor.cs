using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Octokit;
using PortfoliOSS.ModernDomain.Commands;

namespace PortfoliOSS.ModernDomain.Actors
{
    public class GithubWorkerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly GitHubClient _apiClient;
        private readonly ILoggingAdapter _logger;
        private readonly PullRequestRequest _defaultPrRequest;
        private readonly ActorSelection _userManager;
        private readonly ActorSelection _repoManager;
        private readonly ActorSelection _orgManager;
        public IStash Stash { get; set; }

        public GithubWorkerActor(string appName, string apiKey)
        {
            _userManager = Context.ActorSelection(Constants.ActorPaths.USER_MANAGER);
            _repoManager = Context.ActorSelection(Constants.ActorPaths.REPO_MANAGER);
            _orgManager = Context.ActorSelection(Constants.ActorPaths.ORG_MANAGER);


            _defaultPrRequest = new PullRequestRequest { State = ItemStateFilter.All, SortDirection = SortDirection.Ascending, SortProperty = PullRequestSort.Created };

            _logger = Context.GetLogger();
            _logger.Info("Creating a Github actor for {AppName} with key of {ApiKey}", appName, apiKey);

            var tokenAuth = new Credentials(apiKey);
            _apiClient = new GitHubClient(new ProductHeaderValue(appName)) { Credentials = tokenAuth };
            _logger.Info("GithubApiClient created.");

            Become(Normal);
        }
        private void Normal()
        {
            ReceiveAsync<GetMembersOfGithubOrg>(async m =>
            {
                _logger.Info("Getting members of Org {OrgName}", m.GithubOrgName);
                IReadOnlyList<User> users = new List<User>();
                try
                {
                    users = await _apiClient.Organization.Member.GetAll(m.GithubOrgName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during a command");
                }
                var userNames = users.Select(x => new GitHubUser(x.Login, x.Id)).ToList();
                CheckApiLimits(_apiClient.GetLastApiInfo());
                _userManager.Tell(new UsersDiscovered(userNames));
                Sender.Tell(new UsersDiscovered(userNames));
            });


            ReceiveAsync<GetForksAndSourcesForUser>(async m =>
            {
                _logger.Info("Getting forks and sources for {UserName}", m.UserName);
                var result = await _apiClient.Repository.GetAllForUser(m.UserName);
                var forks = result.Where(x => x.Fork).ToList();
                var sources = result.Where(x => !x.Fork).ToList();
                CheckApiLimits(_apiClient.GetLastApiInfo());
                _logger.Info("Sending {ForkCount} forks and {SourceCount} sources to repoManager for {UserName}", forks.Count, sources.Count, m.UserName);
                _repoManager.Tell(new ForkListForUser(m.UserName, forks));
                _repoManager.Tell(new SourceListForUser(m.UserName, sources));
                _logger.Info("Sending {ForkCount} forks and {SourceCount} sources to Sender for {UserName}", forks.Count, sources.Count, m.UserName);
                Sender.Tell(new ForkListForUser(m.UserName, forks));
                Sender.Tell(new SourceListForUser(m.UserName, sources));
            });

            ReceiveAsync<GetForksAndSourcesForOrg>(async m =>
            {
                _logger.Info("Getting forks and sources for org {OrgId}", m.OrgId);
                var result = await _apiClient.Repository.GetAllForOrg(m.OrgName);
                var forks = result.Where(x => x.Fork).ToList();
                var sources = result.Where(x => !x.Fork).ToList();
                CheckApiLimits(_apiClient.GetLastApiInfo());
                _repoManager.Tell(new ForkListForUser(m.OrgName, forks));
                _repoManager.Tell(new SourceListForUser(m.OrgName, sources));
                _logger.Info("Sending {ForkCount} forks and {SourceCount} sources to Sender for {OrgName}",
                    forks.Count, sources.Count, m.OrgName);
            });

            ReceiveAsync<GetSourceForFork>(async m =>
            {
                _logger.Info("Getting Source for Fork: {ForkRepoFullName}", m.Fork.RepoFullName);
                var fullRepoInfo = await _apiClient.Repository.Get(m.Fork.RepoId);
                var sourceRepo = fullRepoInfo.Source;
                CheckApiLimits(_apiClient.GetLastApiInfo());
                var convertedRepo = sourceRepo.ToGitHubRepo();
                _repoManager.Tell(new AddSourceRepoCommand(convertedRepo.RepoFullName, convertedRepo.RepoId, convertedRepo.OrganizationName, convertedRepo.Name, convertedRepo.OrgId));
                _repoManager.Tell(new AddSourceForForkCommand(m.Fork.RepoId, sourceRepo.Id));
            });

            ReceiveAsync<GetPagedRepoPullRequests>(async m =>
            {
                _logger.Info("Getting page {PageNumber} of PRs from repo {RepoFullName}", m.StartingPageNumber, m.RepoFullName);

                var apiOptions = new ApiOptions
                {
                    PageSize = Constants.PR_PAGE_SIZE,
                    StartPage = m.StartingPageNumber,
                    PageCount = 1
                };

                var pullRequests = await _apiClient.PullRequest.GetAllForRepository(m.RepositoryId, _defaultPrRequest, apiOptions);

                CheckApiLimits(_apiClient.GetLastApiInfo());

                if (pullRequests.Any())
                {
                    _logger.Info("PRs found, which means we'll try the next page too");
                    Context.Parent.Tell(new GetPagedRepoPullRequests(m.RepoFullName, m.RepositoryId, m.StartingPageNumber + 1, m.LatestPRNumber), Sender);

                    var formattedInfo = pullRequests.Where(x => x.Number > m.LatestPRNumber).Select(x => new PRInfo(m.RepoFullName, m.RepositoryId, x.Number, x.User.Login, x.Merged, x.MergedAt, x.CreatedAt, x.User.Id)).ToList();
                    _logger.Debug("{FormattedItemCount} formatted items for Page {PageNumber} of repo {RepoName}", formattedInfo.Count, m.StartingPageNumber, m.RepoFullName);

                    _repoManager.Tell(new PRInfoList(formattedInfo, m.RepoFullName));
                    _userManager.Tell(new PRInfoList(formattedInfo, m.RepoFullName));
                }
                else
                {
                    _logger.Info("No PRs found for page {PageNumber} of repo ID {RepoFullName} -- not taking further action", m.StartingPageNumber, m.RepoFullName);
                }
            });

            ReceiveAsync<AddOrgRequest>(async msg =>
            {
                var result = await _apiClient.Organization.Get(msg.OrgName);

                _orgManager.Tell(new AddOrgCommand(result.Login, result.Id));
            });
        }

        private void Paused()
        {
            Receive<Resume>(m =>
            {
                _logger.Info("Resuming github worker activity.");
                Stash.UnstashAll();
                Become(Normal);
            });
            Receive<object>(m =>
            {
                _logger.Info("Received a message but am currently paused; will stash for later retrieval.");
                Stash.Stash();
            });
        }

        private void CheckApiLimits(ApiInfo apiInfo)
        {
            if (apiInfo == null)
            {
                _logger.Warning("Received null ApiInfo -- likely the first request, though.");
                return;
            }

            _logger.Info("We have {RemainingRequests} requests remaining before a reset at {ResetTime}", apiInfo.RateLimit.Remaining, apiInfo.RateLimit.Reset);

            if (apiInfo.RateLimit.Remaining <= Constants.GITHUB_CLIENT_COUNT)
            {
                _logger.Warning("Only {RemainingRequests} requests remaining; pausing until {ResetTime}.", apiInfo.RateLimit.Remaining, apiInfo.RateLimit.Reset);

                var timeSpanDifference = apiInfo.RateLimit.Reset - DateTimeOffset.Now;
                timeSpanDifference = timeSpanDifference + TimeSpan.FromSeconds(5); // add an additional 5 seconds just to be safe.

                _logger.Info("Looks like we'll have to wait for {SecondsToWait} seconds. Scheduling resume message now.", timeSpanDifference.TotalSeconds);
                Context.System.Scheduler.ScheduleTellOnce(timeSpanDifference, Self, new Resume(), Self);
                Become(Paused);
            }
        }

    }
}