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

        IExtractedSubjectInterfaceRepository ExtractedSubject { get; }

        ISubjectRepositoryInterface Subject { get; }

        ICareerProfileRepositoryInterface CareerProfile { get; }

        IPsychometricProfileRepositoryInterface PsychometricProfile { get; }

        IPsychometricAssessmentRepositoryInterface PsychometricAssessment { get; }

        IPlanRepositoryInterface Plan { get; }

        IUserSubscriptionRepositoryInterface UserSubscription { get; }

        IPaymentTransactionRepositoryInterface PaymentTransaction { get; }

        IUsageTransactionRepositoryInterface UsageTransaction { get; }

        ICreditTransactionRepositoryInterface CreditTransaction { get; }

        Task<int> SaveChangesAsync();
    }
}
