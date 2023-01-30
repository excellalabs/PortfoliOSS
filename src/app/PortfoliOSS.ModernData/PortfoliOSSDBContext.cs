using Microsoft.EntityFrameworkCore;

namespace PortfoliOSS.ModernData;

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
        options.UseSqlServer("Server=localhost;Database=portfolioss;User Id=sa;Password=yourStrong(!)Password;Trust Server Certificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Stuff
    }
}