using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class RepoManagerState
{
    public List<GitHubRepo> Sources { get; set; }
    public List<GitHubRepoAndSource> Forks { get; set; }

    public RepoManagerState()
    {
        Sources = new List<GitHubRepo>();
        Forks = new List<GitHubRepoAndSource>();

    }
}