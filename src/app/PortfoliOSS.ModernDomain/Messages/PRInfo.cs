using System;

namespace PortfoliOSS.ModernDomain;

public class PRInfo
{
    public string RepoFullName { get; }
    public long RepoId { get; }
    public int PullRequestId { get; }
    public string AuthorLogin { get; }
    public int AuthorId { get; }
    public bool IsMerged { get; }
    public DateTimeOffset? PRMergeDate { get; }
    public DateTimeOffset CreatedDate { get; }
    public PRInfo(string repoFullName, long repoId, int pullRequestId, string authorLogin, bool isMerged, DateTimeOffset? prMergeDate, DateTimeOffset createdDate, int authorId)
    {
        RepoFullName = repoFullName;
        RepoId = repoId;
        PullRequestId = pullRequestId;
        AuthorLogin = authorLogin;
        IsMerged = isMerged;
        PRMergeDate = prMergeDate;
        CreatedDate = createdDate;
        AuthorId = authorId;
    }
}