using System.Text.RegularExpressions;

namespace Pathly_Helper
{
    /// <summary>
    /// Canonical list of South African universities plus the short forms learners and the model
    /// commonly use ("UCT", "Wits", "Tuks"). Used to detect which institution a free-text career
    /// or roadmap step refers to, so the coherence guard can catch a report that sends a learner
    /// to two different universities at once.
    /// </summary>
    public static class SaUniversities
    {
        public sealed record University(string CanonicalName, IReadOnlyList<string> Aliases);

        public static IReadOnlyList<University> All { get; } = new List<University>
        {
            new("University of Pretoria", new[] { "University of Pretoria", "Pretoria University", "Tuks", "UP" }),
            new("University of Cape Town", new[] { "University of Cape Town", "Cape Town University", "UCT" }),
            new("University of the Witwatersrand", new[] { "University of the Witwatersrand", "Witwatersrand", "Wits", "WITS" }),
            new("University of Johannesburg", new[] { "University of Johannesburg", "Johannesburg University", "UJ" }),
            new("Stellenbosch University", new[] { "Stellenbosch University", "University of Stellenbosch", "Stellenbosch", "Maties", "SU" }),
            new("North-West University", new[] { "North-West University", "North West University", "Potchefstroom University", "NWU" }),
            new("University of KwaZulu-Natal", new[] { "University of KwaZulu-Natal", "University of Kwazulu Natal", "UKZN" }),
            new("University of the Free State", new[] { "University of the Free State", "University of Free State", "UFS" }),
            new("Rhodes University", new[] { "Rhodes University", "Rhodes" }),
            new("University of the Western Cape", new[] { "University of the Western Cape", "UWC" }),
            new("Nelson Mandela University", new[] { "Nelson Mandela University", "Nelson Mandela Metropolitan University", "NMU" }),
            new("University of Limpopo", new[] { "University of Limpopo", "UL" }),
            new("University of South Africa", new[] { "University of South Africa", "UNISA" }),
            new("University of Zululand", new[] { "University of Zululand", "UniZulu" }),
            new("Walter Sisulu University", new[] { "Walter Sisulu University", "WSU" }),
            new("Vaal University of Technology", new[] { "Vaal University of Technology", "VUT" }),
            new("Tshwane University of Technology", new[] { "Tshwane University of Technology", "TUT" }),
            new("Durban University of Technology", new[] { "Durban University of Technology", "DUT" }),
            new("Cape Peninsula University of Technology", new[] { "Cape Peninsula University of Technology", "CPUT" }),
            new("Central University of Technology", new[] { "Central University of Technology", "CUT" }),
            new("Mangosuthu University of Technology", new[] { "Mangosuthu University of Technology", "MUT" }),
            new("Sol Plaatje University", new[] { "Sol Plaatje University", "SPU" }),
            new("University of Mpumalanga", new[] { "University of Mpumalanga", "UMP" }),
            new("Sefako Makgatho Health Sciences University", new[] { "Sefako Makgatho Health Sciences University", "SMU" })
        };

        public static IReadOnlyCollection<string> CanonicalNames { get; } =
            All.Select(u => u.CanonicalName).Distinct(StringComparer.Ordinal).ToArray();

        // Pre-built matchers, ordered longest-alias-first so "University of Pretoria" is preferred
        // over the "UP" acronym when both appear.
        private static readonly IReadOnlyList<(Regex Pattern, string Canonical)> Matchers = BuildMatchers();

        /// <summary>Canonical names of every institution mentioned in the text, in order of appearance.</summary>
        public static IReadOnlyList<string> FindMentions(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var found = new List<(int Index, string Canonical)>();

            foreach (var (pattern, canonical) in Matchers)
            {
                foreach (Match match in pattern.Matches(text))
                {
                    found.Add((match.Index, canonical));
                }
            }

            return found
                .OrderBy(f => f.Index)
                .Select(f => f.Canonical)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The first institution mentioned, or null when the text names none.</summary>
        public static string? FindPrimary(string? text) => FindMentions(text).FirstOrDefault();

        /// <summary>Rewrites every reference to <paramref name="canonicalFrom"/> as <paramref name="canonicalTo"/>.</summary>
        public static string ReplaceMention(string text, string canonicalFrom, string canonicalTo)
        {
            var university = All.FirstOrDefault(u => string.Equals(u.CanonicalName, canonicalFrom, StringComparison.Ordinal));
            if (university is null)
            {
                return text;
            }

            var result = text;

            foreach (var alias in university.Aliases.OrderByDescending(a => a.Length))
            {
                result = BuildPattern(alias).Replace(result, canonicalTo);
            }

            return result;
        }

        private static IReadOnlyList<(Regex, string)> BuildMatchers()
        {
            var matchers = new List<(Regex, string)>();

            foreach (var university in All)
            {
                foreach (var alias in university.Aliases)
                {
                    matchers.Add((BuildPattern(alias), university.CanonicalName));
                }
            }

            return matchers
                .GroupBy(m => m.Item1.ToString(), StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();
        }

        private static Regex BuildPattern(string alias)
        {
            // Acronyms ("UP", "Wits" excluded) are matched case-sensitively so ordinary words like
            // "up" in "keep up your marks" are never mistaken for a university.
            var isAcronym = alias.Length <= 5 && alias.All(c => !char.IsLetter(c) || char.IsUpper(c));

            var options = RegexOptions.Compiled | (isAcronym ? RegexOptions.None : RegexOptions.IgnoreCase);

            return new Regex($@"(?<![\p{{L}}\d]){Regex.Escape(alias)}(?![\p{{L}}\d])", options);
        }
    }
}
