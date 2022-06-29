using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class UserState
{
    public bool IsPartOfOrg { get; set; }
    public List<GitHubRepo> Forks { get; set; }
    public List<GitHubRepo> Sources { get; set; }
    public List<string> MemberOrgs { get; set; }
    public List<GitHubPR> PullRequests { get; set; }

    public UserState()
    {
        MemberOrgs = new List<string>();
        Forks = new List<GitHubRepo>();
        Sources = new List<GitHubRepo>();
        PullRequests = new List<GitHubPR>();
    }
}