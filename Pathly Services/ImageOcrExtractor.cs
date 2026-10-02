using Tesseract;

namespace Pathly_Services
{
    /// <summary>
    /// Free, open-source OCR, used when the upload is a photo/scan of a transcript rather than a
    /// born-digital PDF (see
    /// <see cref="PdfTextExtractor"/> for that case). Tesseract is Apache-2.0 licensed and runs
    /// entirely locally � no per-page cost, no external API call.
    ///
    /// Requires the "eng.traineddata" file to be present in a "tessdata" folder next to the
    /// running executable. See PathlyAI_API/Tessdata-README.md and
    /// PathlyAI_API/tools/Download-TessData.ps1, which already provision this.
    /// </summary>
    internal static class ImageOcrExtractor
    {
        // Phone photos of report cards usually land well below Tesseract's ~300 DPI sweet spot.
        // A 2x upscale before OCR is the single highest-leverage accuracy fix for small print.
        private const float UpscaleFactor = 2.0f;

        // "user_defined_dpi" tells Tesseract the effective resolution of the (already upscaled)
        // image so its character-size heuristics behave � it does not re-rasterize.
        private const int EffectiveDpi = 300;

        // Tolerances for reconstructing table rows from word bounding boxes (mirrors
        // PdfTextExtractor's approach). Rows are grouped by vertical centre; words within a row
        // are sorted left-to-right so a subject and its term marks land on the SAME line.
        private const double FallbackLineTolerance = 3.0;
        private const double LineToleranceScale = 0.35;

        public static OcrResult ExtractText(byte[] fileBytes)
        {
            var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            if (!Directory.Exists(tessDataPath) || !File.Exists(Path.Combine(tessDataPath, "eng.traineddata")))
            {
                throw new InvalidOperationException(
                    "Tesseract language data not found. Run PathlyAI_API/tools/Download-TessData.ps1 " +
                    "to download eng.traineddata into the tessdata folder, then rebuild.");
            }

            // Sharpness gate BEFORE the expensive OCR pass. A photo too blurry to OCR well is
            // rejected here with clear re-shoot guidance instead of wasting a Groq call on garbage.
            if (!ImageQualityAnalyzer.IsSharpEnough(fileBytes))
            {
                return new OcrResult
                {
                    Text = string.Empty,
                    MeanConfidence = 0f,
                    Blurry = true
                };
            }

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default)
            {
                // SparseText is the right mode for multi-column tables (Auto scrambles them into
                // one reading flow), but its raw line output still verticalizes the columns on a
                // bad scan. We therefore reconstruct the rows OURSELVES from the word bounding
                // boxes instead of trusting GetText()'s line breaks.
                DefaultPageSegMode = PageSegMode.SparseText
            };

            engine.SetVariable("user_defined_dpi", EffectiveDpi);

            using var original = Pix.LoadFromMemory(fileBytes);
            using var preprocessed = Preprocess(original);
            using var scaled = ScaleForOcr(preprocessed);
            using var page = engine.Process(scaled);

            var words = CollectWords(page);
            var text = ReconstructTableText(words);

            return new OcrResult
            {
                Text = text,
                MeanConfidence = page.GetMeanConfidence()
            };
        }

        /// <summary>
        /// Walks Tesseract's word-level iterator and collects each word's text plus bounding box.
        /// </summary>
        private static List<OcrWord> CollectWords(Page page)
        {
            var words = new List<OcrWord>();

            using var iterator = page.GetIterator();
            if (iterator is null)
            {
                return words;
            }

            iterator.Begin();

            do
            {
                if (!iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                {
                    continue;
                }

                var text = iterator.GetText(PageIteratorLevel.Word);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                words.Add(new OcrWord
                {
                    Text = text.Trim(),
                    X1 = rect.X1,
                    X2 = rect.X2,
                    Y1 = rect.Y1,
                    Y2 = rect.Y2
                });
            }
            while (iterator.Next(PageIteratorLevel.Word));

            return words;
        }

        /// <summary>
        /// Reconstructs table rows from word bounding boxes: words whose vertical centres are
        /// within tolerance are grouped into one line, then each line is sorted left-to-right.
        /// This is what keeps "Civil Technology (Construction) (Gr 10)" and its "56  40" marks on
        /// the same physical row even when the scan is slightly rotated or the columns are ragged.
        /// </summary>
        private static string ReconstructTableText(List<OcrWord> words)
        {
            if (words.Count == 0)
            {
                return string.Empty;
            }

            var ordered = words
                .OrderBy(w => w.CentreY)
                .ThenBy(w => w.X1)
                .ToList();

            var lines = new List<List<OcrWord>>();

            foreach (var word in ordered)
            {
                var currentLine = lines.Count > 0 ? lines[^1] : null;

                if (currentLine is not null && IsSameRow(currentLine[0], word))
                {
                    currentLine.Add(word);
                }
                else
                {
                    lines.Add(new List<OcrWord> { word });
                }
            }

            var sb = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                sb.Append(string.Join(' ', line.OrderBy(w => w.X1).Select(w => w.Text)));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static bool IsSameRow(OcrWord anchor, OcrWord candidate)
        {
            var anchorHeight = Math.Max(anchor.Y2 - anchor.Y1, 1.0);
            var candidateHeight = Math.Max(candidate.Y2 - candidate.Y1, 1.0);
            var referenceHeight = Math.Max(anchorHeight, candidateHeight);

            var tolerance = Math.Max(referenceHeight * LineToleranceScale, FallbackLineTolerance);

            return Math.Abs(anchor.CentreY - candidate.CentreY) <= tolerance;
        }

        /// <summary>
        /// Grayscale + deskew before OCR. Phone photos of report cards are the main source of
        /// error here (color noise, slight rotation) � both corrections are well-established
        /// Tesseract accuracy improvements and are already bundled in the Tesseract/Leptonica
        /// native libraries, so this costs nothing extra to run. Falls back to the original image
        /// if either step throws (e.g. an already-grayscale or already-1bpp source image).
        /// </summary>
        private static Pix Preprocess(Pix original)
        {
            var working = original;

            try
            {
                working = working.ConvertRGBToGray();
            }
            catch
            {
                // Already grayscale/not RGB � proceed with the original.
            }

            try
            {
                var deskewed = working.Deskew();
                if (!ReferenceEquals(deskewed, working) && !ReferenceEquals(working, original))
                {
                    working.Dispose();
                }
                working = deskewed;
            }
            catch
            {
                // Deskew is best-effort; keep going with whatever we have.
            }

            return working;
        }

        /// <summary>
        /// Bilinear 2x upscale. Must be disposed by the caller (the returned Pix is a new
        /// allocation owned by the caller).
        /// </summary>
        private static Pix ScaleForOcr(Pix source)
        {
            return source.Scale(UpscaleFactor, UpscaleFactor);
        }
    }

    /// <summary>One OCR'd word with its position on the page, used to rebuild table rows.</summary>
    internal class OcrWord
    {
        public string Text { get; set; } = string.Empty;

        public int X1 { get; set; }

        public int X2 { get; set; }

        public int Y1 { get; set; }

        public int Y2 { get; set; }

        public double CentreY => (Y1 + Y2) / 2.0;
    }

    /// <summary>Raw OCR output (layout-preserving text) plus the engine's mean confidence (0-100).</summary>
    internal class OcrResult
    {
        public string Text { get; set; } = string.Empty;

        public float MeanConfidence { get; set; }

        /// <summary>True when the pre-OCR sharpness gate rejected the image as too blurry.</summary>
        public bool Blurry { get; set; }
    }
}
