using System;

namespace PortfoliOSS.ModernDomain;

public record GitHubPR(string RepoFullName, long RepoId, int PullRequestId, string AuthorLogin, bool isMerged,
    DateTimeOffset createdDate, DateTimeOffset? mergedDate);