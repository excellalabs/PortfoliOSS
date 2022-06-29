namespace PortfoliOSS.ModernDomain;

public record GitHubRepo(string RepoFullName, long RepoId, string OrganizationName, string Name, int OrgId);
public record GitHubRepoAndSource(GitHubRepo Repo, long? SourceRepoId);