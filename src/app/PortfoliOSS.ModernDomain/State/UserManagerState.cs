using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class UserManagerState
{
    public List<GitHubUser> Users { get; set; }

    public UserManagerState()
    {
        Users = new List<GitHubUser>();
    }
}