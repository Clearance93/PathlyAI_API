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

            CreateMap<UniversityQualification, UniversityQualificationDto>()
                .ForMember(dest => dest.UniversityQualificationId, opt => opt.Ignore())
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
                .ForMember(dest => dest.AcademicPrediction, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<ExtractedSubject, ExtractedSubjectDto>()
                .ForMember(dest => dest.ExtractionSubjectId, opt => opt.Ignore())
                .ReverseMap();

            // Period blocks are persisted as their own rows with their subject children, so the
            // driver term and the full term history both survive the round trip.
            CreateMap<AcademicPeriod, AcademicPeriodDto>()
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(src => src.Subjects))
                .ReverseMap()
                .ForMember(dest => dest.AcademicPeriodId, opt => opt.Ignore())
                .ForMember(dest => dest.ExtractedAcademicRecord, opt => opt.Ignore());
        }
    }
}
