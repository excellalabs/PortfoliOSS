namespace PortfoliOSS.ModernDomain.Commands;

public record AddForkRepoCommand(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);
public record AddSourceRepoCommand(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);
public record AddSourceForForkCommand(long ForkRepoId, long SourceRepoId);
