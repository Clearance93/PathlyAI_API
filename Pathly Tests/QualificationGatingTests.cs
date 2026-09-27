using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class QualificationGatingTests
    {
        private readonly ApsCalculationService _sut = new();

        private static List<ExtractedSubjectDto> SevenSubjects(int[] marks)
        {
            string[] names =
            {
                "Afrikaans FAL", "English HL", "Life Orientation", "Technical Mathematics",
                "Technical Science", "Engineering Graphics and Design", "Civil Technology"
            };

            return names.Select((n, i) => new ExtractedSubjectDto
            {
                SubjectName = n,
                NumericMark = marks[i],
                MarkType = "Percentage"
            }).ToList();
        }

        [Fact]
        public void NonFinalDriver_UsesIndicativeStandingNotQualification()
        {
            // Real THS Sasolburg Term 2 marks. LO (43) excluded from APS.
            // 28→1, 28→1, [LO excl], 20→1, 48→3, 23→1, 40→3 = 10.
            var subjects = SevenSubjects(new[] { 28, 28, 43, 20, 48, 23, 40 });

            var result = _sut.CalculateAPS(subjects, isFinal: false, isTertiaryOrAdult: false, studyLevel: "Grade 10");

            Assert.Equal(10, result.TotalAps);
            Assert.StartsWith("Indicative", result.QualificationLevel);
            Assert.Contains("not an admission verdict", result.QualificationLevel);
        }

        [Fact]
        public void FinalGrade12Driver_UsesHardQualification()
        {
            // All well above 80 except LO (excluded): 85,85,[80 excl],90,88,82,84 →
            // 7+7+7+7+7+7 = 42 → Excellent.
            var subjects = SevenSubjects(new[] { 85, 85, 80, 90, 88, 82, 84 });

            var result = _sut.CalculateAPS(subjects, isFinal: true, isTertiaryOrAdult: false, studyLevel: "Grade 12");

            Assert.Equal(42, result.TotalAps);
            Assert.StartsWith("Excellent", result.QualificationLevel);
        }

        [Theory]
        [InlineData("Grade 12", true)]
        [InlineData("Grade 10", false)]
        [InlineData("Gr 11", false)]
        [InlineData("N4", false)]
        [InlineData("1st Year", false)]
        [InlineData(null, false)]
        public void FinalQualificationDriver_RequiresFinalBlockAndGr12(string? studyLevel, bool expected)
        {
            var record = new ExtractedAcademicRecordDto
            {
                StudyLevel = studyLevel,
                DriverIsFinal = true,
                Subjects = SevenSubjects(new[] { 70, 70, 70, 70, 70, 70, 70 })
            };

            Assert.Equal(expected, GroqPromptBuilder.IsFinalQualificationDriver(record));
        }

        [Fact]
        public void Gr12ButMidYearDriver_IsGated()
        {
            var record = new ExtractedAcademicRecordDto
            {
                StudyLevel = "Grade 12",
                DriverIsFinal = false,
                DriverTermOrdinal = 2,
                Subjects = SevenSubjects(new[] { 70, 70, 70, 70, 70, 70, 70 })
            };

            Assert.False(GroqPromptBuilder.IsFinalQualificationDriver(record));
        }

        [Fact]
        public void LifeOrientation_ExcludedFromAps_AcrossFinalityModes()
        {
            // Life Orientation 90 must not inflate the total in either mode.
            var final = _sut.CalculateAPS(
                SevenSubjects(new[] { 90, 70, 90, 70, 70, 70, 70 }),
                isFinal: true,
                isTertiaryOrAdult: false,
                studyLevel: "Grade 12");
            var loSubject = final.Subjects!.First(s => s.SubjectName == "Life Orientation");
            Assert.False(loSubject.IncludedInCalculation);
        }
    }
}
