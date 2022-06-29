namespace PortfoliOSS.ModernDomain.Events;

public record SourceAddedForUserEvent(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);