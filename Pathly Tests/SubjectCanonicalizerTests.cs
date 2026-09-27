using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class SubjectCanonicalizerTests
    {
        [Theory]
        [InlineData("Technical Mathematics (Gr 10)", "technical mathematics")]
        [InlineData("Afrikaans First Additional Language (Gr 10)", "afrikaans fal")]
        [InlineData("English Home Language (Gr 10)", "english hl")]
        [InlineData("Civil Technology (Construction) (Gr 10)", "civil technology")]
        [InlineData("Engineering Graphics and Design (Gr 10)", "engineering graphics and design")]
        [InlineData("Technical Mathematics (Gr 11)", "technical mathematics")]
        public void GradeSuffixAndAliases_ResolveToCanonical(string raw, string expected)
        {
            Assert.Equal(expected, SubjectCanonicalizer.ToCanonical(raw));
        }

        [Theory]
        [InlineData("Maths", "mathematics")]
        [InlineData("Math Literacy", "mathematical literacy")]
        [InlineData("Eng Home Lang", "english hl")]
        [InlineData("LO", "life orientation")]
        [InlineData("Life Orient", "life orientation")]
        [InlineData("Physics", "physical sciences")]
        [InlineData("Biology", "life sciences")]
        [InlineData("EGD", "engineering graphics and design")]
        [InlineData("Afrikaans FAL", "afrikaans fal")]
        public void AliasVariants_ResolveToCanonical(string raw, string expected)
        {
            Assert.Equal(expected, SubjectCanonicalizer.ToCanonical(raw));
        }

        [Fact]
        public void UnmappedSubject_IsGradeStrippedAndNormalized()
        {
            // No alias entry, but the "(Gr 10)" suffix must still be stripped so cross-year
            // matching ("Technical Maths (Gr 10)" vs "(Gr 11)") produces the same canonical form.
            Assert.Equal("computer science", SubjectCanonicalizer.ToCanonical("Computer Science (Gr 10)"));
            Assert.Equal("computer science", SubjectCanonicalizer.ToCanonical("Computer Science (Gr 11)"));
        }

        [Fact]
        public void WhitespaceAndCasing_AreNormalized()
        {
            Assert.Equal("mathematics", SubjectCanonicalizer.ToCanonical("  MATHEMATICS "));
        }
    }
}
