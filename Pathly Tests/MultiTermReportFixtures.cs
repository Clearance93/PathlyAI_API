using Pathly_DTOs;

namespace Pathly_Tests
{
    /// <summary>
    /// Builders for a THS Sasolburg-style two-term report card (the exact document shape from the
    /// grill session): 7 subjects x 2 populated terms, learner's mark in the "Final %" column,
    /// a class "Grade Ave %" column that must never be extracted, and a "Level" band column.
    /// </summary>
    public static class MultiTermReportFixtures
    {
        public const string StudentName = "Moeketsi, Rearabetswe";
        public const string LearnerNo = "422215381";
        public const string AdmissionNo = "023169";
        public const string StudyLevel = "Grade 10";
        public const string InstitutionName = "THS Sasolburg";

        // (Subject, Term1 %, Term2 %) — learner's own mark only.
        private static readonly (string Subject, int T1, int T2)[] SubjectRows =
        {
            ("Afrikaans First Additional Language (Gr 10)", 58, 28),
            ("English Home Language (Gr 10)", 37, 28),
            ("Life Orientation (Gr 10)", 64, 43),
            ("Technical Mathematics (Gr 10)", 13, 20),
            ("Technical Science (Gr 10)", 53, 48),
            ("Engineering Graphics and Design (Gr 10)", 32, 23),
            ("Civil Technology (Construction) (Gr 10)", 56, 40)
        };

        public static ExtractedAcademicRecordDto SasolburgGrade10Report()
        {
            var record = new ExtractedAcademicRecordDto
            {
                StudentName = StudentName,
                LearnerNo = LearnerNo,
                AdmissionNo = AdmissionNo,
                StudyLevel = StudyLevel,
                InstitutionName = InstitutionName,
                InstitutionType = "High School",
                AcademicPeriod = "2026",
                AcademicYear = 2026
            };

            record.AcademicPeriods.Add(BuildTerm(1, "Term 1", isFinal: false, row => row.T1));
            record.AcademicPeriods.Add(BuildTerm(2, "Term 2", isFinal: false, row => row.T2));

            // Driver = highest populated non-final term (Term 2) since no Final block exists.
            var driver = record.AcademicPeriods.OrderByDescending(p => p.Ordinal).First();
            record.Subjects = driver.Subjects;
            record.DriverTermOrdinal = 2;
            record.DriverTermLabel = "Term 2";
            record.DriverIsFinal = false;

            return record;
        }

        public static AcademicPeriodDto BuildTerm(
            int ordinal,
            string label,
            bool isFinal,
            Func<(string Subject, int T1, int T2), int> markSelector)
        {
            return new AcademicPeriodDto
            {
                Ordinal = isFinal ? 0 : ordinal,
                Label = label,
                IsFinal = isFinal,
                Subjects = SubjectRows.Select(row => new ExtractedSubjectDto
                {
                    ExtractionSubjectId = Guid.NewGuid(),
                    SubjectName = row.Subject,
                    RawMark = $"{markSelector(row)}%",
                    NumericMark = markSelector(row),
                    Symbol = null,
                    MarkType = "Percentage",
                    TermOrdinal = isFinal ? null : ordinal,
                    TermLabel = isFinal ? null : label,
                    IsFinal = isFinal
                }).ToList()
            };
        }
    }
}
