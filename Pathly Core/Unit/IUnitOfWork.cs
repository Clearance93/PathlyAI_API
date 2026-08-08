using PathlyInterfaces;

namespace Pathly_Core.Unit
{
    public interface IUnitOfWork : IDisposable
    {
        IAcademicRecordRepositoryInterface AcademicRecord { get; }

        IaiResponseRepositoryInterface AiResponse { get; }

        ExtractedAcademicRecordInterfaceRepository ExtractedAcademicRecord { get; }

        IExtractedSubjectInterfaceRepository SubjectExtraction { get; }

        ISubjectResultsRepositoryInterface SubjectResults { get; }

        ICareerMatchRepositoryInterface CareerMatch { get; }

        IApsAnalysisRepositoryInterface ApsAnalysis { get; }

        IDemandingCareerAssessmentRepositoryInterface DemandingCareer { get; }

        IEmploymentOutlookRepositoryInterface EmploymentOutlook { get; }

        IDyingCareerWarningRepositoyInterface DyingCareer { get; }

        IAuthenticationRepository User { get; }

        ExtractedAcademicRecordInterfaceRepository ExtractedAcademicRecord { get; }

        IExtractedSubjectInterfaceRepository ExtractedSubject { get; }

        Task<int> SaveChangesAsync();
    }
}
