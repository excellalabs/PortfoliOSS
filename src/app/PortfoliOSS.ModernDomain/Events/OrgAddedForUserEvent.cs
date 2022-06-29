namespace PortfoliOSS.ModernDomain.Events;

public record OrgAddedForUserEvent(string OrgName, int OrgId, string UserName, int UserId);