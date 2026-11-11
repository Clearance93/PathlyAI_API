namespace Pathly_DTOs
{
    public class SubjectResultsDto
    {
        public Guid SubjectResultId { get; set; }

        public string? Subject { get; set; }

        public int Mark { get; set; }

        /// <summary>NSC APS points derived deterministically from Mark.</summary>
        public int? ApsPoints { get; set; }

        /// <summary>False for subjects (such as Life Orientation) excluded by the APS rules.</summary>
        public bool? IncludedInApsCalculation { get; set; }

        public string? Grade { get; set; }

        public string? CareerRelevance { get; set; }

        public string? ImprovementTip { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
