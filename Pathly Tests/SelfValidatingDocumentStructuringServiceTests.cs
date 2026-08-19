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

        [Fact]
        public async Task ThrowsOnFirstAttempt_SucceedsOnSecond_RetriesInsteadOfCrashing()
        {
            // Reproduces the real-world failure this was built to fix: Groq throwing
            // HttpRequestException (e.g. a 413 token-limit error) shouldn't crash the whole
            // upload — it should be treated like a failed attempt and retried.
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(() => throw new HttpRequestException("RequestEntityTooLarge"));
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.ValidRecord);
            var sut = new SelfValidatingDocumentStructuringService(inner);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(2, inner.CallCount);
            Assert.False(result.NeedsManualReview);
            Assert.NotEmpty(result.Subjects);
        }

        [Fact]
        public async Task ThrowsOnEveryAttempt_ReturnsFlaggedRecordInsteadOfThrowing()
        {
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(() => throw new HttpRequestException("RequestEntityTooLarge"));
            var sut = new SelfValidatingDocumentStructuringService(inner, maxAttempts: 3);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(3, inner.CallCount);
            Assert.True(result.NeedsManualReview);
            Assert.Contains(result.ExtractionWarnings, w => w.Contains("RequestEntityTooLarge"));
        }

        [Fact]
        public async Task InvalidAttemptThenException_StillReturnsBestAttemptFlagged()
        {
            // A partially-useful (but invalid) attempt followed by a hard failure should still
            // surface the earlier structured data rather than discarding it for an empty record.
            var inner = new FakeDocumentStructuringService();
            inner.ResponsesInOrder.Add(FakeDocumentStructuringService.RecordWithDuplicateSubjects);
            inner.ResponsesInOrder.Add(() => throw new HttpRequestException("network blip"));
            var sut = new SelfValidatingDocumentStructuringService(inner, maxAttempts: 2);

            var result = await sut.StructureAcademicRecordAsync(RawText);

            Assert.Equal(2, inner.CallCount);
            Assert.True(result.NeedsManualReview);
            Assert.NotEmpty(result.Subjects);
        }
    }
}
