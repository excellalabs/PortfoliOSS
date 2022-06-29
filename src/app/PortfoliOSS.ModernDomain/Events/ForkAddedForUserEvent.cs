namespace PortfoliOSS.ModernDomain.Events;

public record ForkAddedForUserEvent(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);