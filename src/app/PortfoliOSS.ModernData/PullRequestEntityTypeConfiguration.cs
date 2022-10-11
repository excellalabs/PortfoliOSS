using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PortfoliOSS.ModernData;

public class PullRequestEntityTypeConfiguration : IEntityTypeConfiguration<PullRequest>
{
    public void Configure(EntityTypeBuilder<PullRequest> builder)
    {
        builder.HasKey(x => new { x.PullRequestId, x.Repository.RepoId });
    }
}