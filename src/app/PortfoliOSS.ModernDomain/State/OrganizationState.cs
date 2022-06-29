using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class OrganizationState
{
    public List<GitHubUser> Users { get; set; }

    public OrganizationState()
    {
        Users = new List<GitHubUser>();
    }
}