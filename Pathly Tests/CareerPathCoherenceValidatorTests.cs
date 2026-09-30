using Pathly_DTOs;
using Pathly_Helper;
using Xunit;

namespace Pathly_Tests
{
    public class CareerPathCoherenceValidatorTests
    {
        private static AiResponseDto ResponseWithRoadmap(params string[] roadmap) => new()
        {
            Top3BestCareers = new List<CareerMatchDto>
            {
                new() { Title = "Mechanical Engineer", UniversityCourse = "BEng Mechanical Engineering, University of Pretoria" },
                new() { Title = "Mechatronics Engineer", UniversityCourse = "BEng Mechatronics, University of Pretoria" },
                new() { Title = "Industrial Engineer", UniversityCourse = "BEng Industrial, University of Pretoria" }
            },
            ImprovementtoRoadmap = roadmap.ToList(),
            UniversitiestoConsider = new List<string>()
        };

        [Fact]
        public void Repairs_roadmap_that_sends_learner_to_a_different_university()
        {
            var response = ResponseWithRoadmap(
                "Step 1: Focus on Mathematics and Physical Sciences.",
                "Step 2: Join the robotics club at the University of Cape Town.",
                "Step 3: Complete your first year at the University of Cape Town.");

            var result = CareerPathCoherenceValidator.ValidateAndRepair(response);

            Assert.True(result.Repaired);
            Assert.NotEmpty(result.Warnings);
            Assert.All(response.ImprovementtoRoadmap!, step =>
                Assert.DoesNotContain("Cape Town", step, StringComparison.OrdinalIgnoreCase));
            Assert.Contains("University of Pretoria", response.ImprovementtoRoadmap![1], StringComparison.Ordinal);
            Assert.Contains("University of Pretoria", response.ImprovementtoRoadmap![2], StringComparison.Ordinal);
        }

        [Fact]
        public void Recognises_acronyms_when_aligning_the_roadmap()
        {
            var response = ResponseWithRoadmap("Step 2: Register for the BSc programme at UCT.");

            var result = CareerPathCoherenceValidator.ValidateAndRepair(response);

            Assert.True(result.Repaired);
            Assert.DoesNotContain("UCT", response.ImprovementtoRoadmap![0], StringComparison.Ordinal);
            Assert.Contains("University of Pretoria", response.ImprovementtoRoadmap![0], StringComparison.Ordinal);
        }

        [Fact]
        public void Leaves_a_coherent_report_untouched()
        {
            var response = ResponseWithRoadmap(
                "Step 1: Improve your Mathematics mark.",
                "Step 2: Apply to the University of Pretoria for BEng Mechanical Engineering.");
            response.UniversitiestoConsider = new List<string> { "University of Pretoria" };

            var result = CareerPathCoherenceValidator.ValidateAndRepair(response);

            Assert.False(result.Repaired);
            Assert.False(result.NeedsManualReview);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Flags_a_report_when_no_institution_can_be_identified()
        {
            var response = new AiResponseDto
            {
                Top3BestCareers = new List<CareerMatchDto>
                {
                    new() { Title = "Mechanical Engineer", UniversityCourse = "BEng Mechanical Engineering" }
                },
                ImprovementtoRoadmap = new List<string> { "Step 1: Study hard." }
            };

            var result = CareerPathCoherenceValidator.ValidateAndRepair(response);

            Assert.True(result.NeedsManualReview);
            Assert.NotEmpty(result.Warnings);
        }

        [Theory]
        [InlineData("Keep up your marks this term.", 0)]
        [InlineData("Visit the University of Pretoria open day.", 1)]
        [InlineData("Apply to Wits or UCT.", 2)]
        public void Does_not_treat_ordinary_words_as_universities(string text, int expectedCount)
        {
            Assert.Equal(expectedCount, SaUniversities.FindMentions(text).Count);
        }
    }
}
