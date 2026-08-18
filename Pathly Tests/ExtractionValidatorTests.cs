using Pathly_DTOs;
using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class ExtractionValidatorTests
    {
        private const string SampleRawText = @"
            Student Name: Thabo Nkosi
            School: Sunnyside High School
            Grade 12

            Subject           Mark    Grade
            Mathematics        78%     6
            Physical Sciences  65%     5
            English Home Lang  71%     5
        ";

        [Fact]
        public void ValidRecord_HasNoErrors()
        {
            var record = FakeDocumentStructuringService.ValidRecord();

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ZeroSubjects_IsAnError()
        {
            var record = FakeDocumentStructuringService.EmptyRecord();

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("No subjects"));
        }

        [Fact]
        public void OutOfRangeMark_IsAnError()
        {
            var record = FakeDocumentStructuringService.RecordWithOutOfRangeMark();

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("out-of-range"));
        }

        [Fact]
        public void DuplicateSubjectNames_IsAnError()
        {
            var record = FakeDocumentStructuringService.RecordWithDuplicateSubjects();

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Duplicate subject"));
        }

        [Fact]
        public void MissingSubjectName_IsAnError()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "", NumericMark = 70, MarkType = "Percentage" }
                }
            };

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("missing its name"));
        }

        [Fact]
        public void NeitherMarkNorSymbol_IsAWarningNotAnError()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = null, Symbol = null, MarkType = "Symbol" }
                }
            };

            var result = ExtractionValidator.Validate(SampleRawText, record);

            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void LowSubjectCoverageAgainstRawText_IsAWarningNotAnError()
        {
            // Nine mark-like lines in the raw text, but only one subject extracted.
            var rawText = string.Join("\n", Enumerable.Range(1, 9).Select(i => $"Subject {i}   {60 + i}%"));

            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Subject 1", NumericMark = 61, MarkType = "Percentage" }
                }
            };

            var result = ExtractionValidator.Validate(rawText, record);

            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("some subjects may be missing"));
        }

        [Fact]
        public void SmallDocument_DoesNotTriggerCoverageWarning()
        {
            // Below the 6-line threshold — shouldn't fire the heuristic at all.
            var rawText = "Mathematics 78%\nPhysical Sciences 65%";

            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 78, MarkType = "Percentage" }
                }
            };

            var result = ExtractionValidator.Validate(rawText, record);

            Assert.True(result.IsValid);
            Assert.Empty(result.Warnings);
        }
    }
}
