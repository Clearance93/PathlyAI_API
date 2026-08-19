using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class SelfValidatingDocumentStructuringServiceTests
    {
        private const string RawText = "Mathematics 78%\nPhysical Sciences 65%\nEnglish Home Language 71%";

        [Fact]
        public async Task ValidFirstAttempt_DoesNotRetry()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.ValidRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(1, inner.CallCount);
            Assert.False(result.NeedsManualReview);
        }

        [Fact]
        public async Task InvalidFirstAttempt_ValidSecondAttempt_RetriesOnceAndSucceeds()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.EmptyRecord);
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.ValidRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(2, inner.CallCount);
            Assert.False(result.NeedsManualReview);
            Assert.NotEmpty(result.Subjects);
        }

        [Fact]
        public async Task AlwaysInvalid_ExhaustsRetriesAndFlagsForManualReview()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.EmptyRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner, maxAttempts: 3);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(3, inner.CallCount);
            Assert.True(result.NeedsManualReview);
            Assert.NotEmpty(result.ExtractionWarnings);
        }

        [Fact]
        public async Task DuplicateSubjects_TriggersRetry()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.RecordWithDuplicateSubjects);
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.ValidRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(2, inner.CallCount);
            Assert.False(result.NeedsManualReview);
        }

        [Fact]
        public async Task OutOfRangeMark_TriggersRetry()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.RecordWithOutOfRangeMark);
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.ValidRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(2, inner.CallCount);
            Assert.False(result.NeedsManualReview);
        }

        [Fact]
        public void MaxAttemptsBelowOne_ThrowsArgumentOutOfRange()
        {
            var inner = new FakeDocumentStructuringService();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SelfValidatingDocumentStructuringService(inner, maxAttempts: 0));
        }
    }
}
