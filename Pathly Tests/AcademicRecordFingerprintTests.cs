using Pathly_DTOs;
using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class AcademicRecordFingerprintTests
    {
        private static ExtractedAcademicRecordDto BuildRecord(string studyLevel, params (string Name, int Mark)[] subjects)
        {
            return new ExtractedAcademicRecordDto
            {
                StudyLevel = studyLevel,
                Subjects = subjects.Select(s => new ExtractedSubjectDto
                {
                    SubjectName = s.Name,
                    NumericMark = s.Mark,
                    MarkType = "Percentage"
                }).ToList()
            };
        }

        [Fact]
        public void IdenticalCompleteProfiles_ProduceSameHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75), ("Life Sciences", 80), ("Physical Sciences", 78), ("Home Language", 50));
            var b = BuildRecord("Grade 12", ("Mathematics", 75), ("Life Sciences", 80), ("Physical Sciences", 78), ("Home Language", 50));

            Assert.Equal(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void OneChangedMark_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75), ("Life Sciences", 80), ("Physical Sciences", 78), ("Home Language", 50));
            var b = BuildRecord("Grade 12", ("Mathematics", 75), ("Life Sciences", 80), ("Physical Sciences", 79), ("Home Language", 50));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void DifferentSubject_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75), ("Geography", 80));
            var b = BuildRecord("Grade 12", ("Mathematics", 75), ("History", 80));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void AdditionalSubject_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75));
            var b = BuildRecord("Grade 12", ("Mathematics", 75), ("Geography", 80));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void MissingSubject_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75), ("Geography", 80));
            var b = BuildRecord("Grade 12", ("Mathematics", 75));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void SameSubjectsInDifferentOrder_ProduceSameHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75), ("Geography", 80));
            var b = BuildRecord("Grade 12", ("Geography", 80), ("Mathematics", 75));

            Assert.Equal(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void SubjectCapitalizationDifferences_AreNormalized()
        {
            var a = BuildRecord("Grade 12", ("mathematics", 75));
            var b = BuildRecord("Grade 12", ("MATHEMATICS", 75));

            Assert.Equal(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void WhitespaceDifferences_AreNormalized()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75));
            var b = BuildRecord("Grade 12", ("  Mathematics  ", 75));

            Assert.Equal(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void MarksAreNeverRounded_74And75AreDifferent()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 74));
            var b = BuildRecord("Grade 12", ("Mathematics", 75));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void MarksAreNeverRounded_84And85AreDifferent()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 84));
            var b = BuildRecord("Grade 12", ("Mathematics", 85));

            Assert.NotEqual(AcademicRecordFingerprint.ComputeHash(a), AcademicRecordFingerprint.ComputeHash(b));
        }

        [Fact]
        public void SameAnalysisAndPromptVersion_ProducesSameHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75));

            var hash1 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0");
            var hash2 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ChangedAnalysisVersion_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75));

            var hash1 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0");
            var hash2 = AcademicRecordFingerprint.ComputeHash(a, "2.0", "1.0");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ChangedPromptVersion_ProducesDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 75));

            var hash1 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0");
            var hash2 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "2.0");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void SamePsychometricFingerprint_ProducesSameHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 85));
            var profile = new PsychometricProfileDto { Realistic = 80, Investigative = 90, Artistic = 20, Social = 30, Enterprising = 40, Conventional = 50 };
            var psychHash = PsychometricProfileFingerprint.ComputeHash(profile);

            var hash1 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0", psychHash);
            var hash2 = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0", psychHash);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void SameAcademicsDifferentPsychometrics_ProduceDifferentHash()
        {
            var a = BuildRecord("Grade 12", ("Mathematics", 85), ("Physical Sciences", 80));

            var profileA = new PsychometricProfileDto { Realistic = 90, Investigative = 85, Artistic = 10, Social = 10, Enterprising = 10, Conventional = 10 };
            var profileB = new PsychometricProfileDto { Realistic = 10, Investigative = 10, Artistic = 90, Social = 85, Enterprising = 10, Conventional = 10 };

            var hashA = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0", PsychometricProfileFingerprint.ComputeHash(profileA));
            var hashB = AcademicRecordFingerprint.ComputeHash(a, "1.0", "1.0", PsychometricProfileFingerprint.ComputeHash(profileB));

            Assert.NotEqual(hashA, hashB);
        }
    }
}
