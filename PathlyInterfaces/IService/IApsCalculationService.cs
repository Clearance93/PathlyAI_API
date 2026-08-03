using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    public interface IApsCalculationService
    {
        ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects);

        string GetApsExplanation(int aps);
    } 
}