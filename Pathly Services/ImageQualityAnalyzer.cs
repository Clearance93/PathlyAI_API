using System.Runtime.InteropServices;
using Tesseract;

namespace Pathly_Services
{
    /// <summary>
    /// Measures whether a photo is sharp enough for OCR BEFORE the expensive recognition pass,
    /// using the well-established Laplacian-variance focus metric. A blurry image has low local
    /// intensity variance; a sharp image (with text edges) has high variance. This catches the
    /// "photo too blurry to ever OCR well" case early so the user gets clear re-shoot guidance
    /// instead of a confident-but-garbled extraction.
    /// </summary>
    public static class ImageQualityAnalyzer
    {
        // Empirically-tuned threshold for the variance-of-Laplacian score. Text-heavy documents
        // score far higher than this; truly blurry photos score in the single digits. Held as a
        // field so tests can exercise the metric without hard-coding a second copy.
        internal const double BlurThreshold = 60.0;

        public static bool IsSharpEnough(byte[] fileBytes)
        {
            try
            {
                using var pix = Pix.LoadFromMemory(fileBytes);
                return IsSharpEnough(pix);
            }
            catch
            {
                // If we can't decode/measure (e.g. an unusual format), don't block the upload —
                // let the OCR quality gate downstream be the judge.
                return true;
            }
        }

        public static bool IsSharpEnough(Pix pix)
        {
            try
            {
                // Work on grayscale so colour/chroma variance doesn't mask a blurry luminance.
                using var gray = pix.Depth == 8 ? null : pix.ConvertRGBToGray();
                using var source = gray ?? pix;

                return ComputeLaplacianVariance(source) >= BlurThreshold;
            }
            catch
            {
                return true; // Fail open — the OCR quality gate remains the backstop.
            }
        }

        /// <summary>
        /// Variance of the 3x3 Laplacian over the image's luminance, sampled on a grid for speed.
        /// Requires an 8-bit grayscale Pix. Rows are read three at a time so the vertical kernel
        /// taps are always in memory.
        /// </summary>
        internal static double ComputeLaplacianVariance(Pix gray8)
        {
            var width = gray8.Width;
            var height = gray8.Height;
            if (width < 3 || height < 3)
            {
                return double.MaxValue; // Too small to judge — don't reject.
            }

            var pixData = gray8.GetData();
            var bytesPerRow = pixData.WordsPerLine * 4; // WordsPerLine = 32-bit words per row.
            var basePtr = pixData.Data;

            var prevRow = new byte[bytesPerRow];
            var currRow = new byte[bytesPerRow];
            var nextRow = new byte[bytesPerRow];

            // Preload rows 0 and 1.
            Marshal.Copy(basePtr, prevRow, 0, bytesPerRow);
            Marshal.Copy(basePtr + bytesPerRow, currRow, 0, bytesPerRow);

            var values = new List<double>(1024);
            var step = 2;

            for (var y = 1; y < height - 1; y += step)
            {
                // Load the row below the current one (row y+1).
                if (y + 1 < height)
                {
                    Marshal.Copy(basePtr + ((y + 1) * bytesPerRow), nextRow, 0, bytesPerRow);
                }
                else
                {
                    Array.Clear(nextRow, 0, bytesPerRow);
                }

                for (var x = 1; x < width - 1; x += step)
                {
                    var center = currRow[x];
                    var left = currRow[x - 1];
                    var right = currRow[x + 1];
                    var up = prevRow[x];
                    var down = nextRow[x];

                    // 3x3 Laplacian: 4*center - up - down - left - right
                    var laplacian = (4 * center) - up - down - left - right;
                    values.Add(laplacian);
                }

                // Slide the window down.
                var tmp = prevRow;
                prevRow = currRow;
                currRow = nextRow;
                nextRow = tmp;
            }

            if (values.Count == 0)
            {
                return double.MaxValue;
            }

            var mean = values.Average();
            return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        }
    }
}
