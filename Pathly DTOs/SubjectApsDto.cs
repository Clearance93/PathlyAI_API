namespace Pathly_DTOs
{
    public class SubjectApsDto
    {
        public string SubjectName { get; set; } = string.Empty;

        public int Percentage { get; set; }

        public int ApsPoints { get; set; }

        public bool IncludedInCalculation { get; set; }
    }
}