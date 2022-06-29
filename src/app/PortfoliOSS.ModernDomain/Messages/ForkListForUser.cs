using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace PortfoliOSS.ModernDomain;

public class ForkListForUser
{
    public string UserName { get; }
    public IReadOnlyList<GitHubRepo> Forks { get; }
    public ForkListForUser(string userName, IReadOnlyList<Repository> forks)
    {
        UserName = userName;
        Forks = forks.Select(x=> x.ToGitHubRepo()).ToList();
    }
}

public record AddOrgRequest(string OrgName);