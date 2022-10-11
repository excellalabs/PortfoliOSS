using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfoliOSS.ModernData;

public class Repository
{
    [Required]
    public string Name { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long RepoId { get; set; }

    public List<PullRequest> PullRequests { get; } = new();
}