using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class RepoState
{
    public List<GitHubPR> PullRequests { get; }

    public RepoState()
    {
        PullRequests = new List<GitHubPR>();
    }
}