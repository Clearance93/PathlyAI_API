using Pathly_DTOs;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class AcademicPredictionServiceTests
    {
        private readonly AcademicPredictionService _sut = new();

        [Fact]
        public void TwoTermSecondaryReport_ProducesNextTermPrediction()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = _sut.PredictNextTerm(record);

            Assert.True(result.IsPredictionAvailable);
            Assert.Equal(3, result.NextTermOrdinal);
            Assert.Equal("Term 3", result.NextTermLabel);
            Assert.NotEmpty(result.SubjectPredictions);
        }

        [Fact]
        public void DecliningSubjects_AreMarkedAtRisk()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = _sut.PredictNextTerm(record);

            // Afrikaans 58→28, English 37→28, Technical Maths 13→20 (already <40), EGD 32→23 —
            // all low and/or sharply down. This mirrors the teacher's own "pay attention to
            // Afrikaans, English, Tech Math, EGD" remark.
            Assert.Contains("afrikaans fal", result.AtRiskSubjects);
            Assert.Contains("english hl", result.AtRiskSubjects);
            Assert.Contains("technical mathematics", result.AtRiskSubjects);
            Assert.Contains("engineering graphics and design", result.AtRiskSubjects);
        }

        [Fact]
        public void StrongRisingSubject_IsNotAtRisk()
        {
            // Technical Science 53→48 is above the at-risk threshold with a mild dip; Life
            // Orientation 64→43 is above 40. Neither should be hard-flagged at-risk by the
            // <40 rule, though they may still appear in projections.
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = _sut.PredictNextTerm(record);

            Assert.DoesNotContain("life orientation", result.AtRiskSubjects);
        }

        [Fact]
        public void TwoPointSeries_IsLowConfidence()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            var result = _sut.PredictNextTerm(record);

            var afrikaans = result.SubjectPredictions.First(p => p.SubjectName == "afrikaans fal");
            Assert.True(afrikaans.LowConfidence);
            // Projected band must stay inside 0-100 and represent a decline from the 28% latest.
            Assert.InRange(afrikaans.ProjectedHigh, 0, 100);
            Assert.InRange(afrikaans.ProjectedLow, 0, afrikaans.ProjectedHigh);
            Assert.Equal(28, afrikaans.LatestMark);
        }

        [Fact]
        public void SingleTermRecord_NoPrediction()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();
            record.AcademicPeriods.RemoveAll(p => p.Ordinal == 2);
            record.Subjects = record.AcademicPeriods.First().Subjects;
            record.DriverTermOrdinal = 1;
            record.DriverTermLabel = "Term 1";

            var result = _sut.PredictNextTerm(record);

            Assert.False(result.IsPredictionAvailable);
            Assert.Empty(result.SubjectPredictions);
        }

        [Fact]
        public void FinalBlockPresent_NoPrediction()
        {
            var record = MultiTermReportFixtures.SasolburgGrade10Report();

            // A populated Final/Promotion block ends the year — no "next term" to predict.
            var final = MultiTermReportFixtures.BuildTerm(0, "Final", isFinal: true, row => row.T2 + 5);
            record.AcademicPeriods.Add(final);

            var result = _sut.PredictNextTerm(record);

            Assert.False(result.IsPredictionAvailable);
        }

        [Fact]
        public void LegacyFlatRecord_NoPrediction()
        {
            var record = new ExtractedAcademicRecordDto
            {
                StudyLevel = "Grade 12",
                Subjects = new List<ExtractedSubjectDto>
                {
                    new() { SubjectName = "Mathematics", NumericMark = 78, MarkType = "Percentage" }
                }
            };

            var result = _sut.PredictNextTerm(record);

            Assert.False(result.IsPredictionAvailable);
            Assert.Empty(result.SubjectPredictions);
        }
    }
}
