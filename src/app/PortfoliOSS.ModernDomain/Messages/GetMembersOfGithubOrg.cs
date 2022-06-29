namespace PortfoliOSS.ModernDomain;

public class GetMembersOfGithubOrg
{
    public string GithubOrgName { get; }

    public GetMembersOfGithubOrg(string githubOrgName)
    {
        GithubOrgName = githubOrgName;
    }
}