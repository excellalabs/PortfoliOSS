using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PortfoliOSS.ModernData
{
    // TODO: Extract classes
    public class PortfoliOSSDBContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Repository> Repositories { get; set; }
        public DbSet<PullRequest> PullRequests { get; set; }

        public PortfoliOSSDBContext() { }
        public PortfoliOSSDBContext(DbContextOptions<PortfoliOSSDBContext> options) : base(options) { }

        // TODO: Extract connection string into configuration
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseSqlServer("Server=localhost;Database=portfolioss;User Id=sa;Password=yourStrong(!)Password;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TODO: Stuff
        }
    }

    //public class OrganizationEntityTypeConfiguration : IEntityTypeConfiguration<Organization>
    //{
    //    public void Configure(EntityTypeBuilder<Organization> builder)
    //    {
    //    }
    //}
    public class Organization
    {
        [Required]
        public string Name { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrganizationId { get; set; }

        public List<Repository> Repositories { get; } = new();
        public List<User> Users { get; } = new();
    }

    public class User
    {
        [Required]
        public string Name { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        public List<Repository> Repositories { get; } = new();
        public List<PullRequest> PullRequests { get; } = new();
        public List<Organization> Organizations { get; } = new();
    }

    public class Repository
    {
        [Required]
        public string Name { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RepoId { get; set; }

        public List<PullRequest> PullRequests { get; } = new();
    }

    public class PullRequestEntityTypeConfiguration : IEntityTypeConfiguration<PullRequest>
    {
        public void Configure(EntityTypeBuilder<PullRequest> builder)
        {
            builder.HasKey(x => new { x.PullRequestId, x.Repository.RepoId });
        }
    }
    public class PullRequest
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PullRequestId { get; set; }

        [Required]
        public Repository Repository { get; set; }

        [Required]
        public User Author { get; set; }

        [Required]
        public DateTimeOffset CreatedOn { get; set; }

        [Required]
        public bool IsMerged { get; set; }

        public DateTimeOffset? MergedDate { get; set; }

    }
}