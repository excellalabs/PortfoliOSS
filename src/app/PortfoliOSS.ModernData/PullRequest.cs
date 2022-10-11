using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfoliOSS.ModernData
{
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