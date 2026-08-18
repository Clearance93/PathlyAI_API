using Tesseract;

namespace Pathly_Services
{
    /// <summary>
    /// Free, open-source replacement for Azure Document Intelligence's OCR, used when the upload
    /// is a photo/scan of a transcript rather than a born-digital PDF (see
    /// <see cref="PdfTextExtractor"/> for that case). Tesseract is Apache-2.0 licensed and runs
    /// entirely locally — no per-page cost, no external API call.
    ///
    /// Requires the "eng.traineddata" file to be present in a "tessdata" folder next to the
    /// running executable. See PathlyAI_API/Tessdata-README.md and
    /// PathlyAI_API/tools/Download-TessData.ps1, which already provision this.
    /// </summary>
    internal static class ImageOcrExtractor
    {
        public static string ExtractText(byte[] fileBytes)
        {
            var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            if (!Directory.Exists(tessDataPath) || !File.Exists(Path.Combine(tessDataPath, "eng.traineddata")))
            {
                throw new InvalidOperationException(
                    "Tesseract language data not found. Run PathlyAI_API/tools/Download-TessData.ps1 " +
                    "to download eng.traineddata into the tessdata folder, then rebuild.");
            }

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default)
            {
                // Transcripts are structured (headings + a subject/mark table), not free-flowing
                // prose — Auto lets Tesseract detect that block/column structure itself rather
                // than assuming a single uniform block of text, which measurably helps accuracy
                // on tabular layouts photographed at a slight angle.
                DefaultPageSegMode = PageSegMode.Auto
            };

            using var original = Pix.LoadFromMemory(fileBytes);
            using var preprocessed = Preprocess(original);
            using var page = engine.Process(preprocessed);

            return page.GetText() ?? string.Empty;
        }

        /// <summary>
        /// Grayscale + deskew before OCR. Phone photos of report cards are the main source of
        /// error here (color noise, slight rotation) — both corrections are well-established
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
                // Already grayscale/not RGB — proceed with the original.
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
    }
}
