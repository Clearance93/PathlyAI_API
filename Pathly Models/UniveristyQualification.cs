using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Pathly_Models
{
    public class UniversityQualification
    {
        [Key]
        public Guid UniversityQualificationId { get; set; }

        public string? Name { get; set; }

        public int MinimumAPS { get; set; }

        public string? Status { get; set; }

        public List<string>? RecommendedCourse { get; set; }

        public int Gap { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
