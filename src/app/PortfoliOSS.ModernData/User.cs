using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfoliOSS.ModernData;

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