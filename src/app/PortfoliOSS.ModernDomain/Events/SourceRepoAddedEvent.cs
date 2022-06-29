namespace PortfoliOSS.ModernDomain.Events;

public record SourceRepoAddedEvent(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);