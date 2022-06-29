namespace PortfoliOSS.ModernDomain.Commands;

public record AddUserCommand(string Username, int UserId);
public record AddForkForUserCommand(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);
public record AddSourceForUserCommand(string RepoFullName, long RepoId, string OrganizationName, string RepoName, int OrgId);