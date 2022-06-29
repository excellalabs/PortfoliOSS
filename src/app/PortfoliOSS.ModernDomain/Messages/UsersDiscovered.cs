using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain;

public class UsersDiscovered
{
    public IReadOnlyList<GitHubUser> Users { get; }
    public UsersDiscovered(List<GitHubUser> users)
    {
        Users = users;
    }
}