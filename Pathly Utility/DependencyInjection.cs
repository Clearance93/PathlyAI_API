using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathly_Core;
using Pathly_Core.Pathly_Core;
using Pathly_Core.Unit;
using Pathly_Helper;
using Pathly_Services;
using Pathly_Services.Pathly_Services;
using PathlyInterfaces;
using PathlyInterfaces.IService;
using PathlyRepository;

namespace Pathly_Utility
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDependacy(this IServiceCollection services,
                                                                      IConfiguration configuratio)
        {
            services.AddHttpClient();

            // Typed clients: plain AddScoped<GroqService>()/AddScoped<AzureModelRouterService>()
            // can't resolve a bare HttpClient constructor parameter — this is what actually
            // gives each service a managed HttpClient instance.
            services.AddHttpClient<GroqService>();
            services.AddHttpClient<AzureModelRouterService>();

            services.AddSingleton(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var configExpression = new MapperConfigurationExpression();
                configExpression.AddProfile<PathlyMapper>();

                var configuration = new MapperConfiguration(configExpression, loggerFactory);

                return configuration;
            });

            services.AddSingleton<IMapper>(provider =>
            {
                var configuration = provider.GetRequiredService<MapperConfiguration>();

                return new Mapper(configuration);
            });

            services.Configure<GroqSettings>(configuratio.GetSection("Groq"));
            services.Configure<AzureFoundrySettings>(configuratio.GetSection("AzureOpenAI"));
            services.Configure<CareerMatchWeightsOptions>(configuratio.GetSection("CareerMatchWeights"));

            //Repository
            services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            services.AddScoped<IAcademicRecordRepositoryInterface, AcademicRecordRepository>();
            services.AddScoped<IaiResponseRepositoryInterface, AiResponseRepository>();
            services.AddScoped<ISubjectResultsRepositoryInterface, SubjectResultsRepository>(); 
            services.AddScoped<ICareerMatchRepositoryInterface, CareerMatchRepository>();
            services.AddScoped<IDemandingCareerAssessmentRepositoryInterface,  DemandingCareerAssessmentRepository>();
            services.AddScoped<IEmploymentOutlookRepositoryInterface, EmploymentOutlookRepository>();
            services.AddScoped<IDyingCareerWarningRepositoyInterface, DyingCareerWarningRepository>();
            services.AddScoped<IApsAnalysisRepositoryInterface, ApsAnalysisRepository>();
            services.AddScoped<ExtractedAcademicRecordInterfaceRepository, ExtractedAcademicRecordRepository>();
            services.AddScoped<IExtractedSubjectInterfaceRepository, ExtractedSubjectRepository>();
            services.AddScoped<ISubjectRepositoryInterface, SubjectRepository>();
            services.AddScoped<ICareerProfileRepositoryInterface, CareerProfileRepository>();
            services.AddScoped<IPsychometricProfileRepositoryInterface, PsychometricProfileRepository>();

            //Services
            services.AddScoped<IAuthServiceInterface, AuthenticationService>();
            services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
            services.AddScoped<ICareerAnalysisService, CareerAnalysisService>();
            services.AddScoped<IPremiumCareerAnalysisService, PremiumCareerAnalysisService>();
            services.AddScoped<IApsCalculationService, ApsCalculationService>();
            services.AddScoped<ISubjectKnowledgeService, SubjectKnowledgeService>();
            services.AddScoped<ICareerEvidenceService, CareerEvidenceService>();
            services.AddScoped<IBehavioralSignalService, NoOpBehavioralSignalService>();

            // Groq-only for now (Azure AI Foundry temporarily disabled — everything Azure-related
            // is left in place below, untouched, so this is a one-line flip back to
            // ResilientCareerAiService whenever Azure is wanted again).
            services.AddScoped<IPrimaryCareerAiProvider>(sp => sp.GetRequiredService<GroqService>());
            services.AddScoped<IFallbackCareerAiProvider>(sp => sp.GetRequiredService<AzureModelRouterService>());
            services.AddScoped<IGroqService>(sp => sp.GetRequiredService<GroqService>());
            // services.AddScoped<IGroqService, ResilientCareerAiService>(); // <- re-enable Azure fallback by swapping to this line

            // Document extraction structuring: Groq only, by design (Part: free document
            // extraction). No Azure fallback here — unlike career analysis, this step is cheap
            // and low-stakes enough that we accept occasional retries rather than paying for a
            // paid fallback provider. Wrapped in self-validation/retry so a hallucinated or
            // incomplete extraction gets a second (and third) attempt before it's trusted.
            services.AddScoped<IDocumentStructuringService>(sp =>
                new SelfValidatingDocumentStructuringService(sp.GetRequiredService<GroqService>()));

            //Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

    }
}