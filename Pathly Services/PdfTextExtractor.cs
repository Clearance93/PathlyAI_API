using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pathly_Services
{
    internal static class PdfTextExtractor
    {
        private const double FallbackLineTolerance = 3.0;

        public static string ExtractText(byte[] fileBytes)
        {
            using var document = PdfDocument.Open(fileBytes);
            var sb = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                if (words.Count == 0)
                {
                    continue;
                }

                var lines = GroupIntoLines(words);

                foreach (var line in lines)
                {
                    sb.AppendLine(string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                }

                sb.AppendLine();
            }

            return sb.ToString();
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
