using Pathly_DTOs;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class SubjectResultsReconcilerTests
    {
        [Fact]
        public void Reconcile_UsesExtractedMarksAndLevelsAndKeepsGeneratedGuidance()
        {
            var response = new AiResponseDto
            {
                SubjectResults = new List<SubjectResultsDto>
                {
                    new()
                    {
                        Subject = "Mathematics",
                        Mark = 80,
                        Grade = "Level 5",
                        CareerRelevance = "Strong quantitative foundation",
                        ImprovementTip = "Practice past papers"
                    },
                    new() { Subject = "Invented subject", Mark = 99, Grade = "Level 7" }
                }
            };

            SubjectResultsReconciler.Reconcile(response, new[]
            {
                new ExtractedSubjectDto
                {
                    SubjectName = "Mathematics (Gr 12)",
                    CanonicalSubjectName = "mathematics",
                    NumericMark = 80
                },
                new ExtractedSubjectDto
                {
                    SubjectName = "Accounting",
                    CanonicalSubjectName = "accounting",
                    NumericMark = 33
                },
                new ExtractedSubjectDto
                {
                    SubjectName = "Life Orientation",
                    CanonicalSubjectName = "life orientation",
                    NumericMark = 90
                }
            }, new[]
            {
                new SubjectApsDto { SubjectName = "Mathematics (Gr 12)", ApsPoints = 7, IncludedInCalculation = true },
                new SubjectApsDto { SubjectName = "Accounting", ApsPoints = 2, IncludedInCalculation = true },
                new SubjectApsDto { SubjectName = "Life Orientation", ApsPoints = 7, IncludedInCalculation = false }
            });

            Assert.Collection(response.SubjectResults!,
                math =>
                {
                    Assert.Equal("Mathematics (Gr 12)", math.Subject);
                    Assert.Equal(80, math.Mark);
                    Assert.Equal("Level 7", math.Grade);
                    Assert.Equal("Practice past papers", math.ImprovementTip);
                },
                accounting =>
                {
                    Assert.Equal("Accounting", accounting.Subject);
                    Assert.Equal(33, accounting.Mark);
                    Assert.Equal("Level 2", accounting.Grade);
                    Assert.Equal(2, accounting.ApsPoints);
                    Assert.Null(accounting.CareerRelevance);
                },
                lifeOrientation =>
                {
                    Assert.Equal("Level 7", lifeOrientation.Grade);
                    Assert.Equal(7, lifeOrientation.ApsPoints);
                    Assert.False(lifeOrientation.IncludedInApsCalculation);
                });
        }
    }
}
