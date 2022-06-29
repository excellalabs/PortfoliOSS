using Octokit;

namespace PortfoliOSS.ModernDomain;

public class GetSourceForFork
{
    public GitHubRepo Fork { get; set; }
    public GetSourceForFork(GitHubRepo fork)
    {
        Fork = fork;
    }
}