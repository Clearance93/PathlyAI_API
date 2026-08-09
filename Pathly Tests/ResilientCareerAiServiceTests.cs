using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class ResilientCareerAiServiceTests
    {
        private static ExtractedAcademicRecordDto Record() => new() { Subjects = new() };

        private static ApsResultDto Aps() => new() { TotalAps = 30 };

        [Fact]
        public async Task SuccessfulGroq_DoesNotCallModelRouter()
        {
            var primary = new FakeAiProvider { ResponseFactory = FakeAiProvider.UsableResponse };
            var fallback = new FakeAiProvider { ResponseFactory = FakeAiProvider.UsableResponse };
            var sut = new ResilientCareerAiService(primary, fallback);

            var result = await sut.AnalyzeAcademicRecordAsync(Record(), Aps());

            Assert.NotNull(result);
            Assert.Equal(1, primary.CallCount);
            Assert.Equal(0, fallback.CallCount);
        }

        [Fact]
        public async Task GroqHttpFailure_CallsModelRouter()
        {
            var primary = new FakeAiProvider { ExceptionToThrow = new HttpRequestException("boom") };
            var fallback = new FakeAiProvider { ResponseFactory = FakeAiProvider.UsableResponse };
            var sut = new ResilientCareerAiService(primary, fallback);

            var result = await sut.AnalyzeAcademicRecordAsync(Record(), Aps());

            Assert.NotNull(result);
            Assert.Equal(1, primary.CallCount);
            Assert.Equal(1, fallback.CallCount);
        }

        [Fact]
        public async Task GroqEmptyResponse_CallsModelRouter()
        {
            var primary = new FakeAiProvider { ResponseFactory = FakeAiProvider.UnusableResponse };
            var fallback = new FakeAiProvider { ResponseFactory = FakeAiProvider.UsableResponse };
            var sut = new ResilientCareerAiService(primary, fallback);

            var result = await sut.AnalyzeAcademicRecordAsync(Record(), Aps());

            Assert.NotNull(result);
            Assert.Equal(1, primary.CallCount);
            Assert.Equal(1, fallback.CallCount);
        }

        [Fact]
        public async Task SuccessfulModelRouter_ResponseIsReturned()
        {
            var primary = new FakeAiProvider { ExceptionToThrow = new HttpRequestException("boom") };
            var fallback = new FakeAiProvider { ResponseFactory = FakeAiProvider.UsableResponse };
            var sut = new ResilientCareerAiService(primary, fallback);

            var result = await sut.AnalyzeAcademicRecordAsync(Record(), Aps());

            Assert.Equal("A usable analysis.", result.Summary);
        }

        [Fact]
        public async Task BothProvidersFail_ThrowsControlledFailure_NoResponseReturned()
        {
            var primary = new FakeAiProvider { ExceptionToThrow = new HttpRequestException("groq down") };
            var fallback = new FakeAiProvider { ExceptionToThrow = new HttpRequestException("router down") };
            var sut = new ResilientCareerAiService(primary, fallback);

            await Assert.ThrowsAsync<CareerAnalysisUnavailableException>(
                () => sut.AnalyzeAcademicRecordAsync(Record(), Aps()));
        }

        [Fact]
        public async Task BothProvidersUnusable_ThrowsControlledFailure()
        {
            var primary = new FakeAiProvider { ResponseFactory = FakeAiProvider.UnusableResponse };
            var fallback = new FakeAiProvider { ResponseFactory = FakeAiProvider.UnusableResponse };
            var sut = new ResilientCareerAiService(primary, fallback);

            await Assert.ThrowsAsync<CareerAnalysisUnavailableException>(
                () => sut.AnalyzeAcademicRecordAsync(Record(), Aps()));
        }
    }
}
