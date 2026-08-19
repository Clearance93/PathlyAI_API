using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace Pathly_Tests
{
    /// <summary>
    /// Minimal hand-rolled test double for <see cref="IDocumentStructuringService"/>, so
    /// <see cref="Pathly_Services.SelfValidatingDocumentStructuringService"/> can be tested
    /// without a real Groq call — matches the style of <see cref="FakeAiProvider"/>.
    /// </summary>
    public class FakeDocumentStructuringService : IDocumentStructuringService
    {
        public int CallCount { get; private set; }

        /// <summary>
        /// One factory per call, consumed in order. If calls exceed the queued factories, the
        /// last one is reused — convenient for "always returns the same thing" tests.
        /// </summary>
        public List<Func<ExtractedAcademicRecordDto>> ResponsesInOrder { get; } = new();

        public Task<ExtractedAcademicRecordDto> StructureAcademicRecordAsync(string rawText)
        {
            var index = Math.Min(CallCount, ResponsesInOrder.Count - 1);
            CallCount++;

            if (index < 0)
            {
                return Task.FromResult(new ExtractedAcademicRecordDto());
            }

            return Task.FromResult(ResponsesInOrder[index]());
        }

        public static ExtractedAcademicRecordDto ValidRecord()
        {
            return new ExtractedAcademicRecordDto
            {
                StudentName = "Thabo Nkosi",
                InstitutionName = "Sunnyside High School",
                InstitutionType = "High School",
                StudyLevel = "Grade 12",
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 78, MarkType = "Percentage" },
                    new() { SubjectName = "Physical Sciences", NumericMark = 65, MarkType = "Percentage" },
                    new() { SubjectName = "English Home Language", NumericMark = 71, MarkType = "Percentage" }
                }
            };
        }

        public static ExtractedAcademicRecordDto EmptyRecord()
        {
            return new ExtractedAcademicRecordDto { Subjects = new List<ExtractedSubjectDto>() };
        }

        public static ExtractedAcademicRecordDto RecordWithOutOfRangeMark()
        {
            return new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 145, MarkType = "Percentage" }
                }
            };
        }

        public static ExtractedAcademicRecordDto RecordWithDuplicateSubjects()
        {
            return new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 70, MarkType = "Percentage" },
                    new() { SubjectName = "Mathematics", NumericMark = 72, MarkType = "Percentage" }
                }
            };
        }
    }
}
