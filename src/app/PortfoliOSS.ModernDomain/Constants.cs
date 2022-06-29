using System.Collections.Generic;
using Akka.Actor;
using Octokit;

namespace PortfoliOSS.ModernDomain
{
    public static class ExtensionMethods
    {
        public static GitHubRepo ToGitHubRepo(this Repository thing)
        {
            return new GitHubRepo(thing.FullName, thing.Id, thing.FullName.Split("/")[0], thing.Name, thing.Owner.Id);
        }
    }
    public static class Constants
    {
        public const int PR_PAGE_SIZE = 100;
        public const string APP_NAME = "PortfoliOSS";
        public const string GITHUB_ORG_NAME = "excellaco"; // TODO: Review and extract
        public const int GITHUB_CLIENT_COUNT = 1;

        public class ActorNames
        {
            public const string GITHUB_CLIENT_ACTOR_NAME = "githubClient";
        }

        public static class ActorPaths
        {
            public const string REPO_MANAGER = "/user/RepoManager";
            public const string GITHUB_CLIENT = $"/user/{ActorNames.GITHUB_CLIENT_ACTOR_NAME}";
            public const string USER_MANAGER = "/user/UserManager";
            public const string ORG_MANAGER = "/user/OrgManager";
        }
    }
}