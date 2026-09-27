using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class OcrQualityEvaluatorTests
    {
        // The FIRST failed OCR output from the THS Sasolburg scan — the regression this gate
        // exists to catch. Marks decoupled from rows, broken subject names, totals misread as marks.
        private const string FirstFailedScanText =
            "THS Sasolburg\n" +
            "\n" +
            "   \n" +
            "=1 PRIVATE BAG X ASOLBURG, 104\n" +
            "& 016 \u00a9 = \u00ab\n" +
            "Leamer: MOEKETS| , REARABETSWE - 422215381\n" +
            "Admission N\n" +
            "Torm 1\n" +
            "7 2\n" +
            "5\n" +
            "ER\n" +
            "53 4\n" +
            "s and Design (Gr 10) 22\n" +
            "s 4\n" +
            "\n" +
            "Civil Technalogy (Construction) (Gr 10)\n" +
            "40\n" +
            "230\n" +
            "ES\n" +
            "Not Achieved\n" +
            "4\n";

        // The SECOND scan — after SparseText + upscale it recovered almost every mark, but the
        // photo is low-contrast so per-character confidence is low. This must NOT be hard-rejected;
        // the rows are substantially there.
        private const string SecondScanRecoveredText =
            "THS Sasolhurg\n" +
            "Harry\n" +
            "B3 PRIVATE BAG\n" +
            "Leamer MOEKETS REARABETSWE\n" +
            "Term 1\n" +
            "Subject\n" +
            "Afrikaans First Additional Language (Gr 10) 48 28\n" +
            "English Home Language (Gr 10) 30 64\n" +
            "Technical Mathematics (Gr 10) 13 24 20 22\n" +
            "Technical Science (Gr 10) 53 46 48 37\n" +
            "Engineering Graphics and Design (Gr 10) 32 49 29 58\n" +
            "Civil Technology (Construction) (Gr 10) 56\n" +
            "313 45 230 33\n" +
            "Not Achieved\n";

        [Fact]
        public void FirstFailedScan_IsHardUnusable()
        {
            var result = OcrQualityEvaluator.Evaluate(FirstFailedScanText, meanConfidence: 55f);

            Assert.False(result.IsUsable);
            Assert.True(result.IsLowQuality);
            Assert.NotNull(result.Reason);
        }

        [Fact]
        public void FirstFailedScan_LowConfidence_StillHardUnusable()
        {
            // Low confidence alone no longer hard-rejects, but the structural failure still does.
            var result = OcrQualityEvaluator.Evaluate(FirstFailedScanText, meanConfidence: 20f);

            Assert.False(result.IsUsable);
        }

        [Fact]
        public void SecondScan_WithRecoveredRows_IsUsableEvenAtLowConfidence()
        {
            // This mirrors the user's latest scan: rows substantially intact (subject + marks on
            // the same line) but low per-character confidence. It must be usable (flagged
            // low-quality for manual review), NOT thrown away.
            var result = OcrQualityEvaluator.Evaluate(SecondScanRecoveredText, meanConfidence: 35f);

            Assert.True(result.IsUsable);
            Assert.True(result.IsLowQuality);
            Assert.NotNull(result.Reason);
        }

        [Fact]
        public void CleanTranscript_IsUsable()
        {
            var cleanText =
                "Learner: Moeketsi, Rearabetswe | Learner No: 422215381\n" +
                "Grade 10 | THS Sasolburg\n" +
                "Afrikaans First Additional Language (Gr 10)  58  28\n" +
                "English Home Language (Gr 10)  37  28\n" +
                "Life Orientation (Gr 10)  64  43\n" +
                "Technical Mathematics (Gr 10)  13  20\n" +
                "Technical Science (Gr 10)  53  48\n" +
                "Engineering Graphics and Design (Gr 10)  32  23\n" +
                "Civil Technology (Construction) (Gr 10)  56  40\n";

            var result = OcrQualityEvaluator.Evaluate(cleanText, meanConfidence: 85f);

            Assert.True(result.IsUsable);
            Assert.False(result.IsLowQuality);
            Assert.Null(result.Reason);
        }

        [Fact]
        public void EmptyText_IsUnusable()
        {
            var result = OcrQualityEvaluator.Evaluate("   ", meanConfidence: 90f);

            Assert.False(result.IsUsable);
            Assert.Contains("no readable text", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProseWithNoMarks_IsUnusable()
        {
            var prose = "This is a letter from the school about the upcoming parent evening. " +
                        "Please ensure you attend and bring the learner's report. Regards, The Principal.";

            var result = OcrQualityEvaluator.Evaluate(prose, meanConfidence: 90f);

            Assert.False(result.IsUsable);
            Assert.NotNull(result.Reason);
        }
    }
}
