using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathly_Core;
using Pathly_Core.Pathly_Core;
using Pathly_Core.Unit;
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

            //Services
            services.AddScoped<IAuthServiceInterface, AuthenticationService>();
            services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
            services.AddScoped<ICareerAnalysisService, CareerAnalysisService>();
            services.AddScoped<IApsCalculationService, ApsCalculationService>();

            // Cost-aware AI failover: try Groq first, fall back to Azure Model Router only
            // if Groq fails or returns nothing usable. CareerAnalysisService only ever sees
            // IGroqService, so it doesn't need to know a fallback exists.
            services.AddScoped<IGroqService, ResilientCareerAiService>();

            //Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

    }
}