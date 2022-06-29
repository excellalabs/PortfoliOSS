namespace PortfoliOSS.ModernDomain;

public class GetForksAndSourcesForUser
{
    public string UserName { get; }
    public GetForksAndSourcesForUser(string username)
    {
        UserName = username;
    }
}

public record GetForksAndSourcesForOrg(string OrgName, int OrgId);