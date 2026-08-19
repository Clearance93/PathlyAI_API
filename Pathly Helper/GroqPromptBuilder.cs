using Pathly_DTOs;

namespace Pathly_Helper
{
    public static class GroqPromptBuilder
    {
        /// <summary>
        /// Bump whenever the prompt template changes materially, so cached results generated
        /// under an old prompt stop being served automatically (Part 5).
        /// </summary>
        public const string PromptVersion = "1.0";

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
            Pre-computed career evidence (Pathly calculated these deterministically — use them as the
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
            Psychometric profile (RIASEC, 0-100 each) — factor this into career fit alongside the
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
            9. Never return null — use """" or [] instead.
            10. All *Percentage / chances* / *Outlook* / *Availability* fields are numbers 0-100.
            11. improvementtoRoadmap is always an array of strings, never a single string.
            12. Respond with valid JSON only — no markdown, no commentary, nothing outside the JSON object.
            13. When pre-computed career evidence is provided in the user message, ground your career
                recommendations and their ""reason""/explanation text in that evidence. Do not contradict it
                or invent demand/growth figures that aren't supported by it.
            14. Career guidance is guidance, not a guarantee. Never say a student is ""destined"" or
                ""guaranteed"" to succeed in a career. Prefer phrasing like ""strong match"", ""promising fit"",
                ""good academic alignment"", ""worth exploring"", ""lower current alignment"", or ""alternative pathway"".";
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
        /// Bump whenever the extraction prompt template changes materially — mirrors
        /// <see cref="PromptVersion"/> but scoped to the document-structuring step so the two
        /// can evolve independently.
        /// </summary>
        public const string ExtractionPromptVersion = "1.0";

        public static string BuildDocumentExtractionSystemPrompt()
        {
            return @"You are a precise document-structuring engine for South African academic
            transcripts (NSC/matric report cards, TVET N-course results, Cambridge IGCSE/AS/A-Level
            statements of results, and university transcripts). You are given raw text pulled from a
            PDF or OCR scan — it may have inconsistent spacing, merged words, or jumbled column order.
            Your only job is to find the real data in it and return it as clean structured JSON.

            RULES:
            1. Never invent subjects, marks, names, or institutions that are not actually present in
               the text. If a field cannot be found, use null — never guess or fabricate.
            2. subjects: one entry per subject/course line item that has an associated mark or grade.
               Skip header rows, totals, and rows with no subject name.
            3. markType classification per subject:
               - ""Percentage"": a real percentage, or a ""marks obtained / max marks"" pair (normalize
                 to a 0-100 numericMark by dividing and multiplying by 100, rounded to the nearest whole
                 number; if marks are already out of 100, use them directly).
               - ""GradeEquivalent"": ONLY for Cambridge-style single-letter grades with no numeric score
                 alongside them. Map the letter using this exact table for numericMark:
                 A*=90, A=80, B=70, C=60, D=50, E=40, U=0. Keep the original letter in ""symbol"" and
                 ""rawMark"".
               - ""Symbol"": anything else that can't be resolved to a number (e.g. ""Not achieved"",
                 competency-only outcomes). numericMark is null in this case.
            4. studentName: from labels like ""Student Name"", ""Learner Name"", ""Candidate Name"", ""Name"".
            5. institutionName: from labels like ""School"", ""Institution"", ""University"", ""College"", ""TVET"".
            6. studyLevel: look for patterns like ""Grade 8-12"", ""N2""-""N6"", ""NCV Level 2-4"", ""1st/2nd/3rd/4th
               year"", ""Semester 1/2"", ""Year 1/2/3"".
            7. institutionType, inferred strictly from studyLevel: ""Grade ..."" -> ""High School"";
               ""N2""-""N6"" or ""NCV"" -> ""TVET College""; ""Year ..."" or ""Semester ..."" -> ""University"";
               otherwise -> ""Unknown"".
            8. academicPeriod: the exam year or period if stated (e.g. ""2025"", ""June 2025""), else null.
            9. Respond with valid JSON only — no markdown fences, no commentary, nothing outside the
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
              ""subjects"": [
                {{ ""subjectName"": ""exact subject name as it appears"", ""rawMark"": ""the mark/grade text as it appeared"", ""numericMark"": 0, ""symbol"": ""letter grade or null"", ""markType"": ""Percentage | GradeEquivalent | Symbol"" }}
                /* one object per real subject line item found — do not pad, do not invent */
              ]
            }}";
        }

        /// <summary>
        /// Full prompt builder. <paramref name="careerEvidence"/> is the deterministic evidence
        /// Pathly computed BEFORE calling the AI (Part 9/10/11) — the model is instructed to
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

            var qualifies = apsResult.TotalAps >= 30 ? "true" : "false";
            var shouldRewrite = apsResult.TotalAps < 20 ? "true" : "false";
            var shouldUpgrade = apsResult.TotalAps < 30 ? "true" : "false";
            var subjectCount = record.Subjects.Count;
            var evidenceSection = BuildEvidenceSection(careerEvidence);
            var psychometricSection = BuildPsychometricSection(psychometricProfile);

            return $@"Analyse this specific student. Every field must reference their actual subjects/marks — no generic filler.

            Student: {record.StudentName ?? "Unknown"} | Institution: {record.InstitutionName ?? "Unknown"} ({record.InstitutionType ?? "Unknown"})
            Study Level: {record.StudyLevel ?? "Unknown"} | Period: {record.AcademicPeriod ?? "Unknown"}

            Subjects and Marks:
            {subjectSummary}

            APS Score: {apsResult.TotalAps} | APS Level: {apsResult.QualificationLevel}
            {evidenceSection}{psychometricSection}
            Reference universities (name: minimumAps) — split into qualify/does-not-qualify based on APS {apsResult.TotalAps}:
            University of Pretoria: 30, University of Johannesburg: 28, University of the Witwatersrand: 35,
            University of Cape Town: 36, Stellenbosch University: 35, North-West University: 28, University of KwaZulu-Natal: 30.

            Return ONLY this JSON structure, fully populated for THIS student. Where an array shows one example
            object, generate that many total (see the count noted before each array) — do not just copy the example text.

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
              ""improvementtoRoadmap"": [""Step 1: specific first action"", ""Step 2: mention a real university/bursary"", ""Step 3: first-year focus"", ""Step 4: extracurricular/networking action"", ""Step 5: internship/vacation work in SA""],
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
                /* repeat for all {subjectCount} subjects listed above — one object per subject, exact names */
              ],
              ""top3BestCareers"": [
                {{ ""title"": ""career matching their strongest subjects"", ""reason"": ""based on actual marks"", ""field"": ""SA industry"", ""matchPercentage"": 90, ""requiredSubjects"": ""which of their subjects qualify them"", ""universityCourse"": ""real SA degree name"", ""jobDescription"": ""day to day in SA"", ""growthPotential"": ""SA 2025+ outlook"", ""salaryRange"": ""R xxx 000 - R xxx 000 per annum"", ""timeToQualify"": ""x years"", ""topCompaniesHiring"": [""real SA company"", ""real SA company""] }}
                /* exactly 3 objects total, ranked best to third-best, matchPercentage descending */
              ],
              ""alternativeCareers"": [""alt career 1"", ""alt career 2"", ""alt career 3"", ""alt career 4"", ""alt career 5""],
              ""demandingCareers"": [
                {{ ""careerTitle"": ""high-demand career fitting their subjects"", ""whyitIsInDemand"": ""specific SA 2025 reason"", ""globalDemandLevel"": ""High"", ""salaryRange"": ""R xxx 000 - R xxx 000 per annum"", ""canStudentQualify"": true, ""qualificationVerdict"": ""Yes/No — reasoning tied to APS {apsResult.TotalAps}"", ""reasonForVerdict"": ""based on their marks"", ""chancesifTheyOpt"": 80, ""whatTheyNeedToSuccess"": ""specific steps"", ""honestyMessage"": ""honest, specific"", ""subjectsTheyAreMissing"": [], ""alternativeRoute"": ""non-university entry route"" }}
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
    }
}