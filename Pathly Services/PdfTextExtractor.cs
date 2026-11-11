using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pathly_Services
{
    internal static class PdfTextExtractor
    {
        private const double FallbackLineTolerance = 3.0;
        private const int MaxScannedPagesForOcr = 20;

        public static string ExtractText(byte[] fileBytes, out OcrQualityResult? ocrQuality)
        {
            using var document = PdfDocument.Open(fileBytes);
            var sb = new StringBuilder();
            var ocrConfidences = new List<float>();
            var hasImagePages = false;
            var hadBlurryImagePage = false;
            var scannedPagesSentToOcr = 0;
            var reachedOcrPageLimit = false;

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                if (words.Count > 0)
                {
                    var lines = GroupIntoLines(words);

                    foreach (var line in lines)
                    {
                        sb.AppendLine(string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                    }

                    sb.AppendLine();
                    if (words.Count >= 5)
                    {
                        continue;
                    }
                }

                // PdfPig reads a PDF's text layer but does not render scanned pages. Try the
                // largest embedded image on pages with no text, then run the same local OCR and
                // quality checks used for JPG/PNG uploads.
                var image = page.GetImages()
                    .OrderByDescending(i => (long)i.WidthInSamples * i.HeightInSamples)
                    .FirstOrDefault();

                if (image is null)
                {
                    continue;
                }

                hasImagePages = true;
                if (scannedPagesSentToOcr >= MaxScannedPagesForOcr)
                {
                    reachedOcrPageLimit = true;
                    continue;
                }
                scannedPagesSentToOcr++;

                if (!image.TryGetPng(out var imageBytes))
                {
                    continue;
                }

                var ocr = ImageOcrExtractor.ExtractText(imageBytes);
                if (ocr.Blurry)
                {
                    hadBlurryImagePage = true;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(ocr.Text))
                {
                    sb.AppendLine(ocr.Text.Trim());
                    ocrConfidences.Add(ocr.MeanConfidence);
                }
            }

            var text = sb.ToString();
            if (!hasImagePages)
            {
                ocrQuality = null;
                return text;
            }

            var meanConfidence = ocrConfidences.Count == 0 ? 0 : ocrConfidences.Average();
            ocrQuality = OcrQualityEvaluator.Evaluate(text, meanConfidence);
            if (hadBlurryImagePage && ocrQuality.IsUsable)
            {
                ocrQuality.IsLowQuality = true;
                ocrQuality.Reason = "One or more scanned PDF pages were too blurry to read reliably. " +
                                    "Please double-check the extracted marks or re-scan the document.";
            }
            if (reachedOcrPageLimit && ocrQuality.IsUsable)
            {
                ocrQuality.IsLowQuality = true;
                ocrQuality.Reason = $"Only the first {MaxScannedPagesForOcr} scanned pages were processed. " +
                                    "Please split this large document and upload the remaining pages separately.";
            }

            return text;
        }

        private static List<List<Word>> GroupIntoLines(List<Word> wordsTopToBottom)
        {
            var lines = new List<List<Word>>();

            foreach (var word in wordsTopToBottom)
            {
                var currentLine = lines.Count > 0 ? lines[^1] : null;

                if (currentLine != null && IsSameLine(currentLine[0], word))
                {
                    currentLine.Add(word);
                }
                else
                {
                    lines.Add(new List<Word> { word });
                }
            }

            return lines;
        }

        private static bool IsSameLine(Word lineAnchor, Word candidate)
        {
            // Tolerance scales with glyph height so a document mixing a small-print footnote
            // table with large section headings doesn't have one font size's lines bleed into
            // another's, or a bigger font's own line get incorrectly split in two.
            var anchorHeight = lineAnchor.BoundingBox.Height;
            var candidateHeight = candidate.BoundingBox.Height;
            var referenceHeight = Math.Max(Math.Max(anchorHeight, candidateHeight), 1.0);

            var tolerance = Math.Max(referenceHeight * 0.35, FallbackLineTolerance);

            return Math.Abs(lineAnchor.BoundingBox.Bottom - candidate.BoundingBox.Bottom) <= tolerance;
        }
    }
}
