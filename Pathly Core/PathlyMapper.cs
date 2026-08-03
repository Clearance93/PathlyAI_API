using AutoMapper;
using Pathly_DTOs;
using Pathly_Models;

namespace Pathly_Core
{
    public class PathlyMapper : Profile
    {
        public PathlyMapper()
        {
            CreateMap<ApsAnalysis, ApsAnalysisDto>()
                .ForMember(dest => dest.ApsAnalysisId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<AiResponse, AiResponseDto>()
                .ReverseMap();

            CreateMap<UniveristyQualification, UniversityQualificationDto>()
                .ForMember(dest => dest.UnviversityQualificationId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<CareerMatch, CareerMatchDto>()
                .ReverseMap();

            CreateMap<DemandingCareerAssessment, DemandingCareerAssessmentDto>()
                .ReverseMap();

            CreateMap<DyingCareerWarning, DyingCareerWarningDto>()
                .ReverseMap();

            CreateMap<SubjectResults, SubjectResultsDto>()
                .ForMember(dest => dest.SubjectResultId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<EmploymentOutlook, EmploymentOutlookDto>()
                .ForMember(dest => dest.EmploymentOutlookId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<AcademicRecords, AcademicRecordDtos>().ReverseMap();

            CreateMap<ExtractedAcademicRecord, ExtractedAcademicRecordDto>()
                .ForMember(dest => dest.ExtractedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ExtractionAcademicRecordId, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<ExtractedSubject, ExtractedSubjectDto>()
                .ForMember(dest => dest.ExtractionSubjectId, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
