using Pathly_DTOs;
using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class MultiTermExtractionValidatorTests
    {
        private const string SasolburgRawText = @"
            Learner: Moeketsi, Rearabetswe | Learner No: 422215381
            Grade 10 | THS Sasolburg
            Subject                        Term 1 Final %  Term 2 Final %
            Afrikaans FAL (Gr 10)          58              28
            English HL (Gr 10)             37              28
            Life Orientation (Gr 10)       64              43
            Technical Mathematics (Gr 10)  13              20
            Technical Science (Gr 10)      53              48
            EGD (Gr 10)                    32              23
            Civil Technology (Gr 10)       56              40
        ";

        [Fact]
        public void DuplicateSubjectAcrossTerms_IsValid()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = ExtractionValidator.Validate(SasolburgRawText, record);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void DuplicateSubjectWithinSameTerm_IsStillAnError()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            // Duplicate "Mathematics" inside Term 1.
            record.AcademicPeriods.First(p => p.Ordinal == 1).Subjects.Add(new ExtractedSubjectDto
            {
                SubjectName = "Technical Mathematics (Gr 10)",
                NumericMark = 14,
                MarkType = "Percentage",
                TermOrdinal = 1,
                TermLabel = "Term 1",
                IsFinal = false
            });

            var result = ExtractionValidator.Validate(SasolburgRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Duplicate subject within Term 1"));
        }

        [Fact]
        public void MultiPeriodRecord_AllTermsEmpty_IsAnError()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            // Blank out every period (as if the model returned period shells with no subjects).
            foreach (var period in record.AcademicPeriods)
            {
                period.Subjects.Clear();
            }

            var result = ExtractionValidator.Validate(SasolburgRawText, record);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("No subjects"));
        }

        [Fact]
        public void LegacyFlatRecord_NoPeriods_RemainsValid()
        {
            // Reproduces the pre-multi-term record shape (e.g. from older stored records or the
            // fake structuring service) — must not trip the new period sanity rules.
            var record = new ExtractedAcademicRecordDto
            {
                StudyLevel = "Grade 12",
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 78, MarkType = "Percentage" }
                }
            };

            var result = ExtractionValidator.Validate("Mathematics 78%", record);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void GradeAveColumn_NotPartOfValidationHeuristic()
        {
            // 7 mark-like lines in the raw text, all extracted (Term 2 driver = 7 subjects) →
            // no coverage warning.
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = ExtractionValidator.Validate(SasolburgRawText, record);

            Assert.DoesNotContain(result.Warnings, w => w.Contains("may be missing"));
        }
    }
}
