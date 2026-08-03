namespace Pathly_DTOs
{
    public class DemandingCareerAssessmentDto
    {
        public string? CareerTitle { get; set; }

        public string? WhyitIsInDemand { get; set; }

        public string? GlobalDemandLevel { get; set; }

        public string? SalaryRange { get; set; }

        public bool CanStudentQualify { get; set; }

        public string? QualificationVerdict { get; set; }

        public string? ReasonForVerdict { get; set; }

        public int ChancesifTheyOpt { get; set; }

        public string? WhatTheyNeedToSuccess { get; set; }

        public string? HonestyMessage { get; set; }

        public List<string>? SubjectsTheyAreMissing { get; set; }

        public string? AlternativeRoute { get; set; }

    }
}