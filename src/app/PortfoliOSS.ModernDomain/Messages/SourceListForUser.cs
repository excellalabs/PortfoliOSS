using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace PortfoliOSS.ModernDomain;

public class SourceListForUser
{
    public string UserName { get; }
    public IReadOnlyList<GitHubRepo> Sources { get; }
    public SourceListForUser(string userName, IReadOnlyList<Repository> sources)
    {
        UserName = userName;
        Sources= sources.Select(x => ExtensionMethods.ToGitHubRepo(x)).ToList();
    }
}