using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfoliOSS.ModernData;

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