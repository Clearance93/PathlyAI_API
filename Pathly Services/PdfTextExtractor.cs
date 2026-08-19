using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pathly_Services
{
    /// <summary>
    /// Free, open-source replacement for Azure Document Intelligence's text extraction, for
    /// born-digital PDFs (i.e. PDFs with a real text layer, which covers the vast majority of
    /// exported transcripts). PdfPig is MIT-licensed and has no per-page cost.
    ///
    /// Unlike a naive <c>page.Text</c> read (which is what originally produced unwhitespaced
    /// blobs and broke regex-based parsing), this walks individual words with their bounding
    /// boxes and reconstructs lines by vertical position, then orders words left-to-right within
    /// each line. That keeps table-like layouts (Subject | Mark | Grade columns) readable as
    /// space-separated text, which is what the downstream Groq structuring step needs.
    /// </summary>
    internal static class PdfTextExtractor
    {
        // Fallback tolerance for degenerate cases (zero-height glyphs). Normally we use a
        // per-word tolerance derived from that word's own height instead — see GroupIntoLines —
        // so this scales correctly across small print (fine notes) and large headings, rather
        // than assuming one fixed font size for the whole document.
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
