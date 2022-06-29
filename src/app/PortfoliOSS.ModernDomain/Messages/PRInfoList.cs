using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain;

public class PRInfoList
{
    public IReadOnlyList<PRInfo> PrInfoList { get; }
    public string RepoName { get; }

    public PRInfoList(List<PRInfo> prInfoList, string repoName)
    {
        RepoName = repoName;
        PrInfoList = prInfoList;
    }
}