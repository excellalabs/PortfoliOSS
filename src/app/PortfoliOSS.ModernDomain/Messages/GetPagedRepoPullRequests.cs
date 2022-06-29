namespace PortfoliOSS.ModernDomain;

public class GetPagedRepoPullRequests
{
    public string RepoFullName { get; }
    public long RepositoryId { get; }
    public int StartingPageNumber { get; }
    public int LatestPRNumber { get; }
    public GetPagedRepoPullRequests(string repoFullName, long repositoryId, int startingPageNumber, int latestPrNumber)
    {
        RepoFullName = repoFullName;
        RepositoryId = repositoryId;
        StartingPageNumber = startingPageNumber;
        LatestPRNumber = latestPrNumber;
    }
}