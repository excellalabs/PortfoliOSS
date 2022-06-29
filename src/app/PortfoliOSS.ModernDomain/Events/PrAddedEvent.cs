using System;

namespace PortfoliOSS.ModernDomain.Events;

public record PrAddedEvent(string RepoFullName, long RepoId, int PullRequestId, string AuthorLogin, int AuthorId, bool isMerged,
    DateTimeOffset createdDate, DateTimeOffset? mergedDate);