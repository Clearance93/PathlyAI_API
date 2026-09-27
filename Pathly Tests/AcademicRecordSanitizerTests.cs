using Pathly_DTOs;
using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class AcademicRecordSanitizerTests
    {
        [Fact]
        public void NonsenseSubject_IsRemoved()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 80, MarkType = "Percentage" },
                    new() { SubjectName = "napping", NumericMark = 90, MarkType = "Percentage" }
                }
            };

            var warnings = AcademicRecordSanitizer.Sanitize(record);

            Assert.Single(record.Subjects);
            Assert.Equal("Mathematics", record.Subjects[0].SubjectName);
            Assert.Contains(warnings, w => w.Contains("napping"));
        }

        [Fact]
        public void ReportHeaders_AreRemoved()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Total", NumericMark = 350, MarkType = "Percentage" },
                    new() { SubjectName = "Mathematics", NumericMark = 80, MarkType = "Percentage" }
                }
            };

            AcademicRecordSanitizer.Sanitize(record);

            Assert.Single(record.Subjects);
            Assert.Equal("Mathematics", record.Subjects[0].SubjectName);
        }

        [Fact]
        public void DuplicateSubjectsWithinOneBlock_AreMergedByCanonicalName()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 80, MarkType = "Percentage" },
                    new() { SubjectName = "Maths", NumericMark = 72, MarkType = "Percentage" }
                }
            };

            var warnings = AcademicRecordSanitizer.Sanitize(record);

            Assert.Single(record.Subjects);
            Assert.Contains(warnings, w => w.Contains("duplicate subject", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CanonicalName_IsAssignedToEachSubject()
        {
            var record = new ExtractedAcademicRecordDto
            {
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Maths Literacy", NumericMark = 65, MarkType = "Percentage" }
                }
            };

            AcademicRecordSanitizer.Sanitize(record);

            Assert.Equal("mathematical literacy", record.Subjects[0].CanonicalSubjectName);
        }

        [Fact]
        public void SameSubjectAcrossDifferentTerms_IsNotMerged()
        {
            var record = new ExtractedAcademicRecordDto
            {
                AcademicPeriods = new List<AcademicPeriodDto>
                {
                    new() { Ordinal = 1, Label = "Term 1", Subjects = new List<ExtractedSubjectDto>
                        { new() { SubjectName = "Mathematics", NumericMark = 60, TermOrdinal = 1 } } },
                    new() { Ordinal = 2, Label = "Term 2", Subjects = new List<ExtractedSubjectDto>
                        { new() { SubjectName = "Mathematics", NumericMark = 70, TermOrdinal = 2 } } }
                }
            };

            AcademicRecordSanitizer.Sanitize(record);

            Assert.Single(record.AcademicPeriods[0].Subjects);
            Assert.Single(record.AcademicPeriods[1].Subjects);
        }
    }
}
