using System.Text.RegularExpressions;

namespace Pathly_Services
{
    /// <summary>
    /// Deterministic, post-OCR sanity checks that decide whether the raw text Tesseract produced
    /// is trustworthy enough to send to the structuring step. This is the safety net for the OCR
    /// stage (mirroring what <see cref="Pathly_Helper.ExtractionValidator"/> does for the Groq
    /// structuring stage): a phone photo of a glossy report card can produce column-scrambled,
    /// word-truncated text that LOOKS like a transcript but isn't, and none of that shows up as an
    /// error downstream — it just silently yields 3 corrupted subjects out of 7.
    ///
    /// The evaluator is STRUCTURE-FIRST: the strongest signal is whether enough intact
    /// "subject name + mark on the SAME row" lines survived for Groq to map subjects to marks.
    /// Mean confidence is secondary — a genuinely blurry scan that still yields intact rows is
    /// flagged low-quality rather than hard-rejected, because the data may still be recoverable.
    /// </summary>
    public static class OcrQualityEvaluator
    {
        // An INTACT subject row starts with a real Capitalised word and has a mark at the END of
        // the same line, e.g. "Afrikaans First Additional Language (Gr 10)  58  28". Requiring the
        // line to START with a capital letter (not "=", "&", a digit, or a dangling lowercase
        // fragment) excludes header/address garbage like "=1 PRIVATE BAG ... 104".
        private static readonly Regex IntactSubjectRow = new(
            @"^[A-Z][A-Za-z][A-Za-z\- \(\)&/]{4,}.*\b(\d{1,3}\s*%|\d{1,3})\s*$",
            RegexOptions.Compiled);

        // A subject line that clearly lost its front half — "s and Design (Gr 10)" is the real
        // fingerprint from the first THS failure. Starts with a lowercase 1-3 letter stub.
        private static readonly Regex BrokenSubjectStart = new(
            @"(^|\n)\s*[a-z]{1,3}\s+[A-Za-z]",
            RegexOptions.Compiled);

        public static OcrQualityResult Evaluate(string rawText, float meanConfidence)
        {
            var result = new OcrQualityResult();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.Reason = "The scan produced no readable text. Please photograph the report " +
                                "flat, top-down, in good light, and close enough that the table fills the frame.";
                return result;
            }

            var intactSubjectRows = 0;
            var brokenStarts = 0;

            foreach (var line in rawText.Split('\n'))
            {
                if (IntactSubjectRow.IsMatch(line))
                {
                    intactSubjectRows++;
                }

                if (BrokenSubjectStart.IsMatch(line))
                {
                    brokenStarts++;
                }
            }

            // STRUCTURE FAILURE — the first scan's signature: almost no intact subject+mark rows
            // and/or many broken fragments. This is a hard unusable: Groq cannot map subjects to
            // marks from this, and no amount of prompt tweaking fixes a decoupled table.
            if (intactSubjectRows < 2 && brokenStarts > 0)
            {
                result.IsLowQuality = true;
                result.Reason = "The scan is readable but the subject table did not come through " +
                                "intact — subject names and their marks are not lining up. Please " +
                                "re-photograph the report flat, top-down, with the whole table sharp " +
                                "and in frame, then retry.";
                return result;
            }

            // No intact rows at all and no obvious fragments — probably not a transcript (a letter,
            // a blank page, or OCR that failed completely).
            if (intactSubjectRows < 2)
            {
                result.Reason = "No subject rows with marks could be read from the scan. This may " +
                                "not be a results document, or the table is too far out of focus. " +
                                "Please check the file and try a flat, top-down photo in good light.";
                return result;
            }

            // STRUCTURE OK but confidence low: the rows are there, so the data is likely
            // recoverable — flag for manual review rather than throwing the scan away.
            if (meanConfidence < 45f)
            {
                result.IsLowQuality = true;
                result.IsUsable = true;
                result.Reason = "The subject table was read, but the scan is low-contrast or blurry " +
                                "so some marks may be unreliable. Please double-check the extracted " +
                                "results against the original report.";
                return result;
            }

            result.IsUsable = true;
            return result;
        }
    }
}
