namespace Pathly_DTOs
{
    public class CareerMatchDto
    {
        public string? Title { get; set; }
        
        public string? Reason { get; set; }
        
        public string? Field { get; set; }
        
        public int MatchPercentage { get; set; }
        
        public string? RequiredSubjects { get; set; }  
        
        public string? UniversityCourse { get; set; }
        
        public string? JobDescription { get; set; }
        
        public string? GrowthPotential { get; set; }
        
        public string? SalaryRange { get; set; }
        
        public string? TimeToQualify { get; set; }
        
        public List<string>? TopCompaniesHiring { get; set; }
    }
}