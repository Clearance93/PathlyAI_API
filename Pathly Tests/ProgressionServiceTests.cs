using Pathly_DTOs;
using Pathly_Models;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class ProgressionServiceTests
    {
        private static ExtractedAcademicRecord Record(int year, params (int ordinal, string subject, int mark)[] subjects)
        {
            var period = new AcademicPeriod
            {
                AcademicPeriodId = Guid.NewGuid(),
                Ordinal = subjects[0].ordinal,
                Label = $"Term {subjects[0].ordinal}",
                IsFinal = false,
                Subjects = subjects.Select(s => new ExtractedSubject
                {
                    ExtractionSubjectId = Guid.NewGuid(),
                    SubjectName = s.subject,
                    CanonicalSubjectName = s.subject.ToLowerInvariant(),
                    NumericMark = s.mark
                }).ToList()
            };

            return new ExtractedAcademicRecord
            {
                ExtractionAcademicRecordId = Guid.NewGuid(),
                AcademicYear = year,
                StudyLevel = "Grade 11",
                AcademicPeriods = new List<AcademicPeriod> { period }
            };
        }

        [Fact]
        public void BuildSubjectTrends_classifies_improving_declining_and_steady()
        {
            var records = new List<ExtractedAcademicRecord>
            {
                Record(2025, (1, "Mathematics", 50), (1, "English", 70), (1, "History", 60)),
                Record(2025, (2, "Mathematics", 65), (2, "English", 55), (2, "History", 61))
            };

            var snapshots = ProgressionService.BuildSnapshots(records);
            var trends = ProgressionService.BuildSubjectTrends(snapshots);

            Assert.Equal("improving", trends.Single(t => t.CanonicalName == "mathematics").Direction);
            Assert.Equal("declining", trends.Single(t => t.CanonicalName == "english").Direction);
            Assert.Equal("steady", trends.Single(t => t.CanonicalName == "history").Direction);
            Assert.Equal(15, trends.Single(t => t.CanonicalName == "mathematics").ChangeSinceFirst);
        }

        [Fact]
        public void BuildSnapshots_merges_uploads_and_later_upload_wins_for_same_period()
        {
            // Two separate uploads of the same Term 1 (a re-upload) — the later value must win,
            // not create a duplicate period.
            var records = new List<ExtractedAcademicRecord>
            {
                Record(2025, (1, "Mathematics", 40)),
                Record(2025, (1, "Mathematics", 55)),
                Record(2025, (2, "Mathematics", 60))
            };

            var snapshots = ProgressionService.BuildSnapshots(records);
            var trends = ProgressionService.BuildSubjectTrends(snapshots);

            var maths = trends.Single(t => t.CanonicalName == "mathematics");
            Assert.Equal(2, maths.Points.Count);
            Assert.Equal(55, maths.Points[0].Mark);
            Assert.Equal(60, maths.Points[1].Mark);
        }
    }
}
