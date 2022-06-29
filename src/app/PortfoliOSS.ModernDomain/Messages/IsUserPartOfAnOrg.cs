namespace PortfoliOSS.ModernDomain.Messages;

public class IsUserPartOfAnOrg
{
    public int UserId { get; }
    public IsUserPartOfAnOrg(int userId)
    {
        UserId = userId;
    }
}