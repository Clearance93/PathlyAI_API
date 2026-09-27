using System.Text.RegularExpressions;

namespace Pathly_Helper
{
    /// <summary>
    /// Deterministic mapping from a raw OCR/transcript subject string to a canonical subject
    /// name, so career subject alignment and per-subject trends do not break on suffixes like
    /// "(Gr 10)" or on OCR/alias variants ("Afrikaans First Additional Language" vs "Eng Home
    /// Lang"). Deliberately code, not LLM output — the model keeps the original in
    /// <see cref="Pathly_DTOs.ExtractedSubjectDto.SubjectName"/> and this assigns the canonical form.
    /// </summary>
    public static class SubjectCanonicalizer
    {
        // Strip grade-level / level suffixes that vary across years and would otherwise break
        // cross-term and cross-year matching: "Technical Mathematics (Gr 10)" -> "Technical Mathematics".
        private static readonly Regex GradeSuffixPattern = new(
            @"\s*\(?\s*(?:gr\.?\s*\d{1,2}|grade\s*\d{1,2}|gr\.?\s*\d{1,2}-\d{1,2}|grade\s*\d{1,2}-\d{1,2}|level\s*\d|n\d|ncv\s*level\s*\d)\)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Common alias / OCR-fragment variants -> canonical NSC-style subject name. All lookups
        // are done on the whitespace-collapsed lowercase form (SubjectNormalizer).
        private static readonly Dictionary<string, string> AliasMap = new(StringComparer.Ordinal)
        {
            ["mathematics literacy"] = "mathematical literacy",
            ["maths literacy"] = "mathematical literacy",
            ["math literacy"] = "mathematical literacy",
            ["math lit"] = "mathematical literacy",
            ["mathematical literacy (gr 10)"] = "mathematical literacy",
            ["mathemalics"] = "mathematics",
            ["maths"] = "mathematics",
            ["pure mathematics"] = "mathematics",
            ["afrikaans first additional language"] = "afrikaans fal",
            ["afrikaans fal"] = "afrikaans fal",
            ["afrikaans eerste addisionele taal"] = "afrikaans fal",
            ["afrikaans home language"] = "afrikaans hl",
            ["afrikaans hl"] = "afrikaans hl",
            ["afrikaans huistaal"] = "afrikaans hl",
            ["english first additional language"] = "english fal",
            ["english fal"] = "english fal",
            ["english home language"] = "english hl",
            ["english hl"] = "english hl",
            ["eng home lang"] = "english hl",
            ["eng hl"] = "english hl",
            ["english home lang"] = "english hl",
            ["life orientation"] = "life orientation",
            ["lo"] = "life orientation",
            ["life orient"] = "life orientation",
            ["life or"] = "life orientation",
            ["physical sciences"] = "physical sciences",
            ["physical science"] = "physical sciences",
            ["physics"] = "physical sciences",
            ["chemistry"] = "physical sciences",
            ["life sciences"] = "life sciences",
            ["biology"] = "life sciences",
            ["technical mathematics"] = "technical mathematics",
            ["technical science"] = "technical science",
            ["technical sciences"] = "technical science",
            ["engineering graphics and design"] = "engineering graphics and design",
            ["egd"] = "engineering graphics and design",
            ["engineering graphics design"] = "engineering graphics and design",
            ["civil technology"] = "civil technology",
            ["civil technology (construction)"] = "civil technology",
            ["civil tech"] = "civil technology",
            ["geography"] = "geography",
            ["history"] = "history",
            ["accounting"] = "accounting",
            ["business studies"] = "business studies",
            ["business study"] = "business studies",
            ["economics"] = "economics",
            ["consumer studies"] = "consumer studies",
            ["computer applications technology"] = "computer applications technology",
            ["cat"] = "computer applications technology",
            ["information technology"] = "information technology",
            ["it"] = "information technology",
            ["dramatic arts"] = "dramatic arts",
            ["visual arts"] = "visual arts",
            ["music"] = "music",
            ["religion studies"] = "religion studies",
            ["sepedi home language"] = "sepedi hl",
            ["sepedi fal"] = "sepedi fal",
            ["sesotho home language"] = "sesotho hl",
            ["sesotho fal"] = "sesotho fal",
            ["setswana home language"] = "setswana hl",
            ["setswana fal"] = "setswana fal",
            ["isizulu home language"] = "isizulu hl",
            ["isizulu fal"] = "isizulu fal",
            ["isixhosa home language"] = "isixhosa hl",
            ["isixhosa fal"] = "isixhosa fal",
            ["tshivenda home language"] = "tshivenda hl",
            ["tshivenda fal"] = "tshivenda fal",
            ["xitsonga home language"] = "xitsonga hl",
            ["xitsonga fal"] = "xitsonga fal",
            ["siswati home language"] = "siswati hl",
            ["siswati fal"] = "siswati fal",
            ["isindebele home language"] = "isindebele hl",
            ["isindebele fal"] = "isindebele fal"
        };

        public static string ToCanonical(string? rawSubjectName)
        {
            if (string.IsNullOrWhiteSpace(rawSubjectName))
            {
                return SubjectNormalizer.Normalize(rawSubjectName);
            }

            var trimmed = rawSubjectName.Trim();
            var stripped = GradeSuffixPattern.Replace(trimmed, string.Empty).Trim();

            // Try alias lookup on the stripped form first (e.g. "Afrikaans FAL"), then on the
            // raw normalized form so grade-less OCR variants still resolve.
            var strippedKey = SubjectNormalizer.Normalize(stripped);
            if (AliasMap.TryGetValue(strippedKey, out var strippedMatch))
            {
                return strippedMatch;
            }

            var rawKey = SubjectNormalizer.Normalize(trimmed);
            if (AliasMap.TryGetValue(rawKey, out var rawMatch))
            {
                return rawMatch;
            }

            // Unmapped subject: keep the grade-stripped name as the canonical form. This still
            // fixes the cross-year "(Gr 10)"/"(Gr 11)" drift even without an alias entry.
            return SubjectNormalizer.Normalize(string.IsNullOrWhiteSpace(stripped) ? trimmed : stripped);
        }

        // Keywords that appear inside genuine SA/Cambridge curriculum subject names. A name that
        // contains one of these (or is a mapped alias) is treated as a real subject.
        private static readonly HashSet<string> SubjectKeywords = new(StringComparer.Ordinal)
        {
            "language", "languages", "mathematics", "maths", "math", "science", "sciences",
            "studies", "study", "technology", "technologies", "arts", "art", "accounting",
            "accountancy", "economics", "geography", "history", "biology", "physics", "chemistry",
            "orientation", "computer", "information", "business", "music", "drama", "design",
            "engineering", "agriculture", "agricultural", "tourism", "hospitality", "religion",
            "technical", "civil", "graphics", "visual", "consumer", "literacy", "electrical",
            "mechanical", "entrepreneurship", "sepedi", "sesotho", "setswana", "isizulu", "isixhosa",
            "tshivenda", "xitsonga", "siswati", "isindebele", "afrikaans", "english", "zulu", "xhosa",
            "sotho", "tswana", "venda", "tsonga", "swati", "ndebele", "equine", "marine", "hospitality",
            "needlework", "woodwork", "metalwork", "drawing", "drafting", "computing", "statistics",
            "culinary", "dance", "sport", "geometric", "technical", "electrotechnics", "fitting",
            "welding", "plumbing", "bricklaying", "carpentry", "motor", "hairdressing", "travel",
            "office", "administration", "public", "relations", "media", "film", "photography", "graphic"
        };

        // Words/phrases that appear on a report card but are never a subject themselves.
        private static readonly HashSet<string> NonSubjectTokens = new(StringComparer.Ordinal)
        {
            "total", "totals", "average", "avg", "grade", "grades", "level", "levels", "mark",
            "marks", "symbol", "percentage", "percent", "subject", "subjects", "learner", "name",
            "school", "term", "year", "class", "position", "aggregate", "overall", "final",
            "promotion", "admission", "remarks", "comment", "comments", "result", "results",
            "achievement", "n/a", "na", "none", "pass", "passed", "fail", "failed", "absent",
            "achieved", "not achieved", "subject name", "marks obtained", "mark obtained", "maximum",
            "minimum", "out of", "date", "signature", "examination", "exam", "report"
        };

        private static readonly Regex MarkLikePattern = new(
            @"^[\d\s.,%+-]+$",
            RegexOptions.Compiled);

        /// <summary>
        /// True when the given raw subject string looks like a genuine school/college subject.
        /// Used to reject the OCR/LLM artefacts (a stray verb like "napping", a header such as
        /// "Total", or a bare number) that would otherwise be treated as a scored subject and
        /// inflate the APS and career matches.
        /// </summary>
        public static bool IsRecognisedSubject(string? rawSubjectName)
        {
            if (string.IsNullOrWhiteSpace(rawSubjectName))
            {
                return false;
            }

            var canonical = ToCanonical(rawSubjectName);

            if (KnownCanonicalSubjects.Contains(canonical))
            {
                return true;
            }

            if (AliasMap.ContainsKey(SubjectNormalizer.Normalize(rawSubjectName)))
            {
                return true;
            }

            return LooksLikeRealSubject(rawSubjectName);
        }

        private static bool LooksLikeRealSubject(string rawSubjectName)
        {
            var trimmed = rawSubjectName.Trim();

            if (trimmed.Length < 3 || trimmed.Length > 60)
            {
                return false;
            }

            if (!trimmed.Any(char.IsLetter) || MarkLikePattern.IsMatch(trimmed))
            {
                return false;
            }

            var normalized = SubjectNormalizer.Normalize(trimmed);

            if (NonSubjectTokens.Contains(normalized))
            {
                return false;
            }

            var words = trimmed.Split(new[] { ' ', '\t', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return false;
            }

            // A known subject keyword anywhere in the name is a strong signal.
            if (words.Any(w => SubjectKeywords.Contains(SubjectNormalizer.Normalize(w))))
            {
                return true;
            }

            // A multi-word, alphabetic name (e.g. "Sepedi Home Language", "Business Studies").
            // Anything multi-word that survived the non-subject filter is plausibly a subject.
            if (words.Length >= 2 && words.All(w => w.Length >= 2))
            {
                return true;
            }

            // A lone unmapped word is far more likely to be OCR noise than a subject.
            return false;
        }

        /// <summary>Canonical names Pathly recognises as real subjects (the alias targets).</summary>
        public static IReadOnlyCollection<string> KnownCanonicalSubjects { get; } =
            AliasMap.Values.Distinct(StringComparer.Ordinal).ToArray();
    }
}
