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

            //Services
            services.AddScoped<IAuthServiceInterface, AuthenticationService>();
            services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
            services.AddScoped<ICareerAnalysisService, CareerAnalysisService>();
            services.AddScoped<IApsCalculationService, ApsCalculationService>();

            services.AddScoped<IGroqService, AzureModelRouterService>();

            //Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

    }
}