namespace PortfoliOSS.ModernDomain.Events;

public record ForkRepoAddedEvent(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);