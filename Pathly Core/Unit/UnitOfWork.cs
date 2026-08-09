using Pathly_Data;
using PathlyInterfaces;

namespace Pathly_Core.Unit
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _Context;

        public IAcademicRecordRepositoryInterface AcademicRecord { get ; private set ; }

        public IAuthenticationRepository User { get; private set; }

        public IaiResponseRepositoryInterface AiResponse { get; private set; }

        public ISubjectResultsRepositoryInterface SubjectResults { get; private set; }

        public ICareerMatchRepositoryInterface CareerMatch { get; private set; }

        public IDemandingCareerAssessmentRepositoryInterface DemandingCareer { get; private set; }

        public IEmploymentOutlookRepositoryInterface EmploymentOutlook { get; private set; }

        public IDyingCareerWarningRepositoyInterface DyingCareer { get; private set; }

        public IApsAnalysisRepositoryInterface ApsAnalysis { get; private set; }

        public ExtractedAcademicRecordInterfaceRepository ExtractedAcademicRecord { get; private set; }

        public IExtractedSubjectInterfaceRepository ExtractedSubject { get; private set; }

        public ISubjectRepositoryInterface Subject { get; private set; }

        public ICareerProfileRepositoryInterface CareerProfile { get; private set; }

        public IPsychometricProfileRepositoryInterface PsychometricProfile { get; private set; }

        public UnitOfWork(ApplicationDbContext context,
                          IAcademicRecordRepositoryInterface academicRecord,
                          IAuthenticationRepository user,
                          IaiResponseRepositoryInterface aiResponse,
                          IDyingCareerWarningRepositoyInterface dyingCareer,
                          IEmploymentOutlookRepositoryInterface emplymentOutlook,
                          IDemandingCareerAssessmentRepositoryInterface demandingCareer,
                          ICareerMatchRepositoryInterface careerMatch,
                          ISubjectResultsRepositoryInterface subjectResults,
                          IApsAnalysisRepositoryInterface apsAnalysis,
                          ExtractedAcademicRecordInterfaceRepository extractedAcademicRecord,
                          IExtractedSubjectInterfaceRepository extractedSubject,
                          ISubjectRepositoryInterface subject,
                          ICareerProfileRepositoryInterface careerProfile,
                          IPsychometricProfileRepositoryInterface psychometricProfile)
        {
            _Context = context;
            AcademicRecord = academicRecord ?? throw new ArgumentNullException(nameof(academicRecord));
            User = user ?? throw new ArgumentNullException(nameof(user));
            AiResponse = aiResponse ?? throw new ArgumentNullException(nameof(aiResponse));
            DyingCareer = dyingCareer ?? throw new ArgumentNullException(nameof(dyingCareer));
            EmploymentOutlook = emplymentOutlook ?? throw new ArgumentNullException(nameof(emplymentOutlook));
            DemandingCareer = demandingCareer ?? throw new ArgumentNullException(nameof(demandingCareer));
            CareerMatch = careerMatch ?? throw new ArgumentNullException(nameof(careerMatch));
            SubjectResults = subjectResults ?? throw new ArgumentNullException(nameof(subjectResults));
            ApsAnalysis = apsAnalysis ?? throw new ArgumentNullException(nameof(apsAnalysis));
            ExtractedAcademicRecord = extractedAcademicRecord ?? throw new ArgumentNullException(nameof(extractedAcademicRecord));
            ExtractedSubject = extractedSubject ?? throw new ArgumentNullException(nameof(extractedSubject));
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            CareerProfile = careerProfile ?? throw new ArgumentNullException(nameof(careerProfile));
            PsychometricProfile = psychometricProfile ?? throw new ArgumentNullException(nameof(psychometricProfile));
        }

        public void Dispose()
        {
            _Context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _Context.SaveChangesAsync();
        }
    }
}
