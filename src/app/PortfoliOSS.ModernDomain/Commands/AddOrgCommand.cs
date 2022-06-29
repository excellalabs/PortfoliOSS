using System;
using Microsoft.Identity.Client;

namespace PortfoliOSS.ModernDomain.Commands;

public record AddOrgCommand(string OrgName, int OrgId);

public record AddOrgForUserCommand(string OrgName, int OrgId, string UserName, int UserId);

public record AddPRCommand(string RepoFullName, long RepoId, int PullRequestId, string AuthorLogin, bool isMerged,
    DateTimeOffset createdDate, DateTimeOffset? mergedDate, int AuthorUserId);