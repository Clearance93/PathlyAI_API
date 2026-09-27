using Pathly_DTOs;

namespace Pathly_Helper
{
    public static class GroqPromptBuilder
    {
        /// <summary>
        /// Bump whenever the prompt template changes materially, so cached results generated
        /// under an old prompt stop being served automatically (Part 5).
        /// </summary>
        public const string PromptVersion = "2.1";

        private static string BuildEvidenceSection(IReadOnlyList<CareerEvidenceDto>? careerEvidence)
        {
            if (careerEvidence is null || careerEvidence.Count == 0)
            {
                return string.Empty;
            }

            var lines = careerEvidence
                .OrderByDescending(e => e.OverallScore)
                .Select(e =>
                    $"- {e.CareerName} ({e.Category}): AcademicFit={e.AcademicFit}, SubjectAlignment={e.SubjectAlignment}, " +
                    $"PsychometricFit={(e.PsychometricFit?.ToString() ?? "n/a")}, CareerDemand={e.CareerDemand}, " +
                    $"FutureGrowth={e.FutureGrowth}, OverallScore={e.OverallScore:0.0}");

            return $@"
            Pre-computed career evidence (Pathly calculated these deterministically � use them as the
            basis for top3BestCareers/demandingCareers/alternativeCareers. Do NOT invent career facts,
            demand levels, or growth outlooks that contradict this evidence. Explain WHY a career is a
            match by referencing these specific dimensions, e.g. ""strong academic fit and high current
            demand"" rather than just naming the career):
            {string.Join("\n            ", lines)}
";
        }

        private static string BuildPsychometricSection(PsychometricProfileDto? profile)
        {
            if (profile is null)
            {
                return string.Empty;
            }

            return $@"
            Psychometric profile (RIASEC, 0-100 each) � factor this into career fit alongside the
            academic evidence above, and explain how the two reinforce or diverge from each other:
            Realistic={profile.Realistic}, Investigative={profile.Investigative}, Artistic={profile.Artistic},
            Social={profile.Social}, Enterprising={profile.Enterprising}, Conventional={profile.Conventional}
";
        }

        public static string BuildSystemPrompt()
        {
            return @"You are PathlyAI, a South African career guidance counsellor with 20 years
            of experience across township schools, private schools, TVET colleges and universities.
            Give brutally honest, specific, non-generic guidance grounded in the real 2025/2026 SA job market.

            RULES:
            1. subjectResults: one entry per subject listed, exact subject name, never blank.
            2. qualifiesForUniversity = true only if APS >= 30.
            3. dyingCareerWarnings: exactly 3 distinct items, never empty.
            4. demandingCareers: exactly 3 distinct items, all fields populated, no nulls.
            5. top3BestCareers: exactly 3 distinct items. employmentOutlooks: exactly 2 distinct items.
            6. universitiesTheyQualifyFor / universitiesTheyDoNotQualifyFor: real minimumAps values, never 0.
            7. userStrength / userWeaknesses: name specific subjects by their actual scores.
            8. All salaries in ZAR (e.g. ""R180 000 - R250 000 per annum"").
            9. Never return null � use """" or [] instead.
            10. All *Percentage / chances* / *Outlook* / *Availability* fields are numbers 0-100.
            11. improvementtoRoadmap is always an array of strings, never a single string.
            12. Respond with valid JSON only � no markdown, no commentary, nothing outside the JSON object.
            13. When pre-computed career evidence is provided in the user message, ground your career
                recommendations and their ""reason""/explanation text in that evidence. Do not contradict it
                or invent demand/growth figures that aren't supported by it.
            14. Career guidance is guidance, not a guarantee. Never say a student is ""destined"" or
                ""guaranteed"" to succeed in a career. Prefer phrasing like ""strong match"", ""promising fit"",
                ""good academic alignment"", ""worth exploring"", ""lower current alignment"", or ""alternative pathway"".
            15. COHERENCE — the whole report must describe ONE coherent plan per career, never a mix:
                - improvementtoRoadmap must be a single logical progression toward the top recommended
                  career. Name ONE institution and ONE qualification, and stay consistent across every
                  step. Never advise enrolling at two different universities, or combining activities
                  that belong to different institutions, fields or cities.
                - If a roadmap step mentions a university, it MUST be the same institution named in the
                  top recommended career's universityCourse. Do not introduce a second university.
                - Every career's universityCourse must be a single, real SA programme at one named
                  institution, and must not contradict universitiesTheyQualifyFor / universitiesTheyDoNotQualifyFor.
                - A learner cannot be simultaneously doing a degree at one university and a course at
                  another; pick the realistic single path and describe that.";
        }

        private static string FormatSubjectLine(ExtractedSubjectDto s)
        {
            // Cambridge-style results: we've estimated a percentage from the letter grade for
            // scoring purposes, but the model should reason from the real grade, not the estimate.
            if (s.MarkType == "GradeEquivalent" && s.NumericMark.HasValue)
            {
                return $"- {s.SubjectName}: Grade {s.Symbol} (internal estimate ~{s.NumericMark}% for scoring)";
            }

            if (s.NumericMark.HasValue)
            {
                return $"- {s.SubjectName}: {s.NumericMark}%";
            }

            return $"- {s.SubjectName}: {s.Symbol}";
        }

        public static string BuildUserPrompt(ExtractedAcademicRecordDto record, ApsResultDto apsResult)
        {
            return BuildUserPrompt(record, apsResult, null, null);
        }

        /// <summary>
        /// Bump whenever the extraction prompt template changes materially � mirrors
        /// <see cref="PromptVersion"/> but scoped to the document-structuring step so the two
        /// can evolve independently.
        /// </summary>
        public const string ExtractionPromptVersion = "2.0";

        public static string BuildDocumentExtractionSystemPrompt()
        {
            return @"You are a precise document-structuring engine for South African academic
            transcripts (NSC/matric report cards, TVET N-course results, Cambridge IGCSE/AS/A-Level
            statements of results, and university transcripts). You are given raw text pulled from a
            PDF or OCR scan � it may have inconsistent spacing, merged words, or jumbled column order.
            Your only job is to find the real data in it and return it as clean structured JSON.

            RULES:
            1. Never invent subjects, marks, names, or institutions that are not actually present in
               the text. If a field cannot be found, use null � never guess or fabricate.
            2. Report cards often show MULTIPLE TERMS side by side (e.g. columns for Term 1, Term 2,
               Term 3, Term 4, and a Final/Promotion column). Group the data per term in ""periods"".
               - Each populated term block becomes one entry in ""periods"" with its own subjects.
               - A subject repeated across terms appears once per term, in that term's subjects.
               - Skip term blocks that are entirely blank/not yet filled in.
               - If a single-column transcript or result slip has no term structure at all, return a
                 single entry in ""periods"" with ordinal 0, label ""Overall"", isFinal true, and the
                 subjects in it � do NOT leave ""periods"" empty.
            3. In a term block, include one subject per line item that has an associated mark.
               Skip header rows, totals, averages, and rows with no subject name.
            4. COLUMN SELECTION � this is critical. A learner's report card frequently shows BOTH the
               learner's own mark AND the class/grade average. Extract ONLY the learner's own result:
               - The learner's mark is the ""%""/""Final %""/""Mark"" column (e.g. ""58"" or ""58%"").
               - ""Grade Ave %"", ""Class Average"", or similar is the CLASS average � NEVER extract it.
               - ""Level"" columns (1-7 NSC achievement levels) are bands derived from the mark � skip
                 them, do not treat them as a score. Keep only the percentage/mark as numericMark.
               - If a row shows only a Level with no percentage, you may keep it as symbol only with
                 numericMark null rather than inventing a percentage.
            5. markType classification per subject:
               - ""Percentage"": a real percentage, or a ""marks obtained / max marks"" pair (normalize
                 to a 0-100 numericMark by dividing and multiplying by 100, rounded to the nearest whole
                 number; if marks are already out of 100, use them directly).
               - ""GradeEquivalent"": ONLY for Cambridge-style single-letter grades with no numeric score
                 alongside them. Map the letter using this exact table for numericMark:
                 A*=90, A=80, B=70, C=60, D=50, E=40, U=0. Keep the original letter in ""symbol"" and
                 ""rawMark"".
               - ""Symbol"": anything else that can't be resolved to a number (e.g. ""Not achieved"",
                 competency-only outcomes). numericMark is null in this case.
            6. studentName: from labels like ""Student Name"", ""Learner Name"", ""Candidate Name"", ""Name"".
            7. institutionName: from labels like ""School"", ""Institution"", ""University"", ""College"", ""TVET"".
            8. studyLevel: look for patterns like ""Grade 8-12"", ""N2""-""N6"", ""NCV Level 2-4"", ""1st/2nd/3rd/4th
               year"", ""Semester 1/2"", ""Year 1/2/3"".
            9. institutionType, inferred strictly from studyLevel: ""Grade ..."" -> ""High School"";
               ""N2""-""N6"" or ""NCV"" -> ""TVET College""; ""Year ..."" or ""Semester ..."" -> ""University"";
               otherwise -> ""Unknown"".
            10. academicPeriod: the exam year or period if stated (e.g. ""2025"", ""2026""), else null.
               Do NOT put a term label in academicPeriod � terms live in ""periods"".
            11. learnerNo/admissionNo: portal learner number / admission number when present (e.g. under
               ""Learner No"" / ""Admission No""), else null.
            12. Respond with valid JSON only � no markdown fences, no commentary, nothing outside the
                JSON object.";
        }

        public static string BuildDocumentExtractionUserPrompt(string rawText)
        {
            return $@"Extract the structured academic record from this raw transcript text.

            RAW TEXT:
            ---
            {rawText}
            ---

            Return ONLY this JSON structure:

            {{
              ""studentName"": ""string or null"",
              ""institutionName"": ""string or null"",
              ""institutionType"": ""High School | TVET College | University | Unknown"",
              ""studyLevel"": ""string or null"",
              ""academicPeriod"": ""string or null"",
              ""learnerNo"": ""string or null"",
              ""admissionNo"": ""string or null"",
              ""periods"": [
                {{ ""ordinal"": 1, ""label"": ""Term 1"", ""isFinal"": false, ""subjects"": [
                  {{ ""subjectName"": ""exact subject name as it appears"", ""rawMark"": ""the mark/grade text as it appeared"", ""numericMark"": 0, ""symbol"": ""letter grade or null"", ""markType"": ""Percentage | GradeEquivalent | Symbol"" }}
                ] }}
                /* one object per populated term block. A subject that appears in multiple terms is
                   listed once inside each term's subjects. Skip blank/not-yet-filled term columns.
                   If the document has no term structure, return ONE entry with ordinal 0,
                   label ""Overall"", isFinal true, holding all subjects. */
              ]
            }}";
        }

        /// <summary>
        /// Full prompt builder. <paramref name="careerEvidence"/> is the deterministic evidence
        /// Pathly computed BEFORE calling the AI (Part 9/10/11) � the model is instructed to
        /// explain this evidence, not invent its own career facts. <paramref name="psychometricProfile"/>
        /// is only present for premium (Layer 2) analyses.
        /// </summary>
        public static string BuildUserPrompt(
            ExtractedAcademicRecordDto record,
            ApsResultDto apsResult,
            IReadOnlyList<CareerEvidenceDto>? careerEvidence,
            PsychometricProfileDto? psychometricProfile)
        {
            var subjectSummary = record.Subjects.Any()
                ? string.Join("\n", record.Subjects.Select(FormatSubjectLine))
                : "No subjects extracted.";

            // A non-final / non-final-year driver gets indicative standing only � no hard
            // "qualifies / does not qualify" verdict, no university admit lists. The model is told
            // the reference universities are for context only in that case.
            var isGated = !IsFinalQualificationDriver(record);
            var qualifies = !isGated && apsResult.TotalAps >= 30 ? "true" : "false";
            var shouldRewrite = !isGated && apsResult.TotalAps < 20 ? "true" : "false";
            var shouldUpgrade = !isGated && apsResult.TotalAps < 30 ? "true" : "false";
            var subjectCount = record.Subjects.Count;
            var evidenceSection = BuildEvidenceSection(careerEvidence);
            var psychometricSection = BuildPsychometricSection(psychometricProfile);
            var predictionSection = BuildPredictionSection(record);
            var driverBlockLabel = BuildDriverLabel(record);
            var universityListInstruction = isGated
                ? "NOTE: this is a mid-year/non-final report (not final NSC results). Do NOT emit a hard " +
                  "qualifies/does-not-qualify university verdict. universitiesTheyQualifyFor and " +
                  "universitiesTheyDoNotQualifyFor must be [] (empty), and apsAnalysis.qualifiesForUniversity " +
                  "must be false. Phrase qualification language as indicative/current standing only."
                : $"Reference universities (name: minimumAps) � split into qualify/does-not-qualify based on APS {apsResult.TotalAps}:";

            var referenceUniversities = isGated
                ? "University of Pretoria: 30, University of Johannesburg: 28, University of the Witwatersrand: 35, University of Cape Town: 36, Stellenbosch University: 35, North-West University: 28, University of KwaZulu-Natal: 30."
                : string.Empty;

            return $@"Analyse this specific student. Every field must reference their actual subjects/marks � no generic filler.

            Student: {record.StudentName ?? "Unknown"} | Institution: {record.InstitutionName ?? "Unknown"} ({record.InstitutionType ?? "Unknown"})
            Study Level: {record.StudyLevel ?? "Unknown"} | Driver period: {driverBlockLabel} | Year/Period: {record.AcademicPeriod ?? "Unknown"}

            Subjects and Marks (from the driver period above):
            {subjectSummary}

            APS Score: {apsResult.TotalAps} | APS Level: {apsResult.QualificationLevel}
            {evidenceSection}{psychometricSection}{predictionSection}
            {universityListInstruction}
            {referenceUniversities}

            Return ONLY this JSON structure, fully populated for THIS student. Where an array shows one example
            object, generate that many total (see the count noted before each array) � do not just copy the example text.

            {{
              ""overallScore"": 0.0,
              ""academicPersonality"": ""specific to this student's subject mix and marks"",
              ""summary"": ""3-sentence summary citing their actual subjects and scores"",
              ""feedBack"": ""cite their strongest and weakest subject by name, honestly"",
              ""motivationalMessage"": ""mention their actual APS of {apsResult.TotalAps}"",
              ""userStrength"": [""their top subject and why"", ""their 2nd top subject and what it opens up"", ""another specific strength""],
              ""userWeaknesses"": [""their lowest subject, honest feedback"", ""another subject to improve and why""],
              ""studyTips"": [""tip for weakest subject by name"", ""tip for 2nd weakest subject"", ""general tip for their subject combo""],
              ""skillsToLearn"": [""skill complementing their strongest subject"", ""skill for their top recommended career"", ""a digital/technical skill relevant in SA""],
              ""fiveYearsOutLook"": ""specific 5-year outlook given APS {apsResult.TotalAps} and their subjects"",
              ""salaryRange"": ""ZAR range for their top recommended career after qualifying"",
              ""riskAssessment"": ""honest career risks specific to their subjects and the SA economy"",
              ""subjectChangeSuggestion"": ""specific change, or state clearly none is needed"",
              ""improvementtoRoadmap"": [""Step 1: specific first action"", ""Step 2: name the SAME institution as the top recommended career's universityCourse"", ""Step 3: first-year focus at that same institution"", ""Step 4: extracurricular/networking action at that same institution"", ""Step 5: internship/vacation work in SA in the same field""],
              ""apsAnalysis"": {{
                ""totalAps"": {apsResult.TotalAps},
                ""apsExplanation"": ""{apsResult.QualificationLevel}"",
                ""qualifiesForUniversity"": {qualifies},
                ""qualificationMessage"": ""what APS {apsResult.TotalAps} means for realistic programme choices"",
                ""universitiesTheyQualifyFor"": [ {{ ""name"": ""from reference list above"", ""minimumAps"": 0, ""status"": ""Qualifies"" }} /* one object per university they qualify for */ ],
                ""universitiesTheyDoNotQualifyFor"": [ {{ ""name"": ""from reference list above"", ""minimumAps"": 0, ""status"": ""Does Not Qualify"" }} /* one object per university they don't qualify for, or [] if they qualify everywhere */ ],
                ""improvementAdvice"": {{
                  ""shouldReWriteMatric"": {shouldRewrite},
                  ""shouldUpgradeSubjects"": {shouldUpgrade},
                  ""recommendedSubjectsToImprove"": [""subject to improve first"", ""subject to improve second""],
                  ""alternativeOptions"": [""TVET programme relevant to their subjects"", ""bridging course at a real SA university"", ""learnership/internship option""],
                  ""motivationalGuidance"": ""specific to their results and APS {apsResult.TotalAps}""
                }}
              }},
              ""subjectResults"": [
                {{ ""subject"": ""exact subject name"", ""mark"": 0, ""grade"": ""Level 1-7"", ""careerRelevance"": ""specific to this subject in SA"", ""improvementTip"": ""specific tip for this subject"" }}
                /* repeat for all {subjectCount} subjects listed above � one object per subject, exact names */
              ],
              ""top3BestCareers"": [
                {{ ""title"": ""career matching their strongest subjects"", ""reason"": ""based on actual marks"", ""field"": ""SA industry"", ""matchPercentage"": 90, ""requiredSubjects"": ""which of their subjects qualify them"", ""universityCourse"": ""real SA degree name"", ""jobDescription"": ""day to day in SA"", ""growthPotential"": ""SA 2025+ outlook"", ""salaryRange"": ""R xxx 000 - R xxx 000 per annum"", ""timeToQualify"": ""x years"", ""topCompaniesHiring"": [""real SA company"", ""real SA company""] }}
                /* exactly 3 objects total, ranked best to third-best, matchPercentage descending */
              ],
              ""alternativeCareers"": [""alt career 1"", ""alt career 2"", ""alt career 3"", ""alt career 4"", ""alt career 5""],
              ""demandingCareers"": [
                {{ ""careerTitle"": ""high-demand career fitting their subjects"", ""whyitIsInDemand"": ""specific SA 2025 reason"", ""globalDemandLevel"": ""High"", ""salaryRange"": ""R xxx 000 - R xxx 000 per annum"", ""canStudentQualify"": true, ""qualificationVerdict"": ""Yes/No � reasoning tied to APS {apsResult.TotalAps}"", ""reasonForVerdict"": ""based on their marks"", ""chancesifTheyOpt"": 80, ""whatTheyNeedToSuccess"": ""specific steps"", ""honestyMessage"": ""honest, specific"", ""subjectsTheyAreMissing"": [], ""alternativeRoute"": ""non-university entry route"" }}
                /* exactly 3 distinct objects total, each a different career */
              ],
              ""dyingCareerWarnings"": [
                {{ ""careerTitle"": ""a genuinely declining SA career"", ""whyItIsDying"": ""automation/outsourcing/policy reason"", ""jobAvailabilityIn5Years"": 25, ""chanceOfGettingJobAfterStudying"": 20, ""honestWarning"": ""brutally honest"", ""motivationalRedirect"": ""toward a better-fit career"", ""betterAlternative"": ""specific alternative career"", ""isRelevantToStudent"": true, ""relevanceReason"": ""tied to their subject combination"" }}
                /* exactly 3 distinct objects total; not all need isRelevantToStudent = true */
              ],
              ""employmentOutlooks"": [
                {{ ""careerTitle"": ""one of their top3BestCareers"", ""chanceOfEmploymentAfterGraduation"": 85, ""averageTimeToGetFirstJob"": ""x months"", ""jobMarketCompetition"": ""High/Medium/Low"", ""southAfricanMarketInsight"": ""specific 2025 insight"", ""globalOpportunities"": ""specific for SA graduates"", ""topIndustriesHiring"": [""industry 1"", ""industry 2"", ""industry 3""], ""entryLevelSalary"": ""R xxx 000 per annum"", ""seniorLevelSalary"": ""R xxx 000 per annum"", ""outlookSummary"": ""5-year outlook for this career in SA"" }}
                /* exactly 2 objects total, matching their top 2 recommended careers */
              ],
              ""bursariesAvailable"": [""real SA bursary relevant to their field"", ""second real SA bursary"", ""third real SA bursary""],
              ""universitiestoConsider"": [""University of Pretoria"", ""University of Cape Town"", ""University of the Witwatersrand"", ""University of Johannesburg"", ""Stellenbosch University"", ""North-West University"", ""University of KwaZulu-Natal""]
            }}";
        }

        /// <summary>
        /// Hard qualification verdicts are reserved for a FINAL block on a FINAL secondary year
        /// (Gr 12 NSC). A mid-year term or any pre-Grade-12 report is indicative standing only �
        /// SA universities admit on final NSC results.
        /// </summary>
        public static bool IsFinalQualificationDriver(ExtractedAcademicRecordDto record)
        {
            if (record is null)
            {
                return true; // Legacy unknown � keep old behaviour rather than surprise users.
            }

            if (!record.DriverIsFinal)
            {
                return false;
            }

            var studyLevel = record.StudyLevel;
            if (string.IsNullOrWhiteSpace(studyLevel))
            {
                return false;
            }

            var level = SubjectNormalizer.Normalize(studyLevel);
            return level == "grade 12"
                || level == "gr 12"
                || level == "matric"
                || level == "nsc"
                || level.StartsWith("grade 12", StringComparison.Ordinal)
                || level.StartsWith("gr 12", StringComparison.Ordinal);
        }

        private static string BuildDriverLabel(ExtractedAcademicRecordDto record)
        {
            if (record is null)
            {
                return "Unknown";
            }

            if (record.DriverIsFinal)
            {
                return record.DriverTermLabel ?? "Final / Overall";
            }

            if (!string.IsNullOrWhiteSpace(record.DriverTermLabel))
            {
                return record.DriverTermLabel;
            }

            return record.DriverTermOrdinal is > 0 ? $"Term {record.DriverTermOrdinal}" : "Latest term";
        }

        private static string BuildPredictionSection(ExtractedAcademicRecordDto record)
        {
            if (record?.AcademicPrediction is null || !record.AcademicPrediction.IsPredictionAvailable)
            {
                return string.Empty;
            }

            var prediction = record.AcademicPrediction;
            var lines = prediction.SubjectPredictions
                .OrderBy(p => p.LatestMark)
                .Select(p =>
                    $"- {p.SubjectName}: latest {p.LatestMark}%, projected {p.Direction} to {p.ProjectedLow}-{p.ProjectedHigh}% " +
                    $"{(p.LowConfidence ? "(low confidence � two terms only)" : string.Empty)}");

            var atRisk = prediction.AtRiskSubjects.Count > 0
                ? string.Join(", ", prediction.AtRiskSubjects)
                : "none currently flagged";

            return $@"
            Next-term projection ({prediction.NextTermLabel}): Pathly computed these deterministically from the
            learner's uploaded term history. Treat them as estimates for planning � never as guaranteed marks.
            {string.Join("\n            ", lines)}

            At-risk subjects to prioritise in improvement advice: {atRisk}
            ({prediction.Caveat})
";
        }
    }
}
