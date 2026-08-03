using Pathly_DTOs;

namespace Pathly_Helper
{
    public static class GroqPromptBuilder
    {
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
            12. Respond with valid JSON only — no markdown, no commentary, nothing outside the JSON object.";
        }

        public static string BuildUserPrompt(ExtractedAcademicRecordDto record, ApsResultDto apsResult)
        {
            var subjectSummary = record.Subjects.Any()
                ? string.Join("\n", record.Subjects.Select(s =>
                    s.NumericMark.HasValue
                        ? $"- {s.SubjectName}: {s.NumericMark}%"
                        : $"- {s.SubjectName}: {s.Symbol}"))
                : "No subjects extracted.";

            var qualifies = apsResult.TotalAps >= 30 ? "true" : "false";
            var shouldRewrite = apsResult.TotalAps < 20 ? "true" : "false";
            var shouldUpgrade = apsResult.TotalAps < 30 ? "true" : "false";
            var subjectCount = record.Subjects.Count;

            return $@"Analyse this specific student. Every field must reference their actual subjects/marks — no generic filler.

            Student: {record.StudentName ?? "Unknown"} | Institution: {record.InstitutionName ?? "Unknown"} ({record.InstitutionType ?? "Unknown"})
            Study Level: {record.StudyLevel ?? "Unknown"} | Period: {record.AcademicPeriod ?? "Unknown"}

            Subjects and Marks:
            {subjectSummary}

            APS Score: {apsResult.TotalAps} | APS Level: {apsResult.QualificationLevel}

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