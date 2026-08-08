using AutoMapper;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Models;
using PathlyInterfaces.IService;
using System.Text.RegularExpressions;

namespace Pathly_Services
{
    public class DocumentExtractionService : IDocumentExtractionService
    {
        private readonly DocumentIntelligenceClient _Client;
        private readonly IUnitOfWork _unit;
        private readonly IMapper _mapper;

        private static readonly Dictionary<string, string[]> FieldLabelVariants = new()
        {
            ["StudentName"] = new[] { "Student Name", "Learner Name", "Candidate Name", "Name" },
            ["School"] = new[] { "School", "Institution", "University", "College", "TVET" },
        };

        public DocumentExtractionService(IConfiguration config,
                                         IUnitOfWork unit,
                                         IMapper mapper)
        {
            var endpoint = config["AzureDocumentIntelligence:Endpoint"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence:Endpoint is not configured.");
            var key = config["AzureDocumentIntelligence:Key"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence:Key is not configured.");

            _Client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(key));
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ExtractedAcademicRecordDto> ExtractAcademicRecordAsync(string base64File, string mimeType, string? fileName)
        {
            var fileBytes = Convert.FromBase64String(base64File);

            var operation = await _Client.AnalyzeDocumentAsync(
                WaitUntil.Completed, "prebuilt-layout", BinaryData.FromBytes(fileBytes));

            var result = operation.Value;

            var record = new ExtractedAcademicRecordDto
            {
                Subjects = await ExtractSubjectsFromTables(result.Tables),
                RawExtractedText = result.Content,
                ExtractedAt = DateTime.Now
            };

            ExtractFieldsFromParagraphs(result, record);

            return record;
        }

        private async Task<List<ExtractedSubjectDto>> ExtractSubjectsFromTables(IReadOnlyList<DocumentTable> tables)
        {
            var subjectTable = tables.FirstOrDefault(t =>
            {
                var headerRow = t.Cells.Where(c => c.RowIndex == 0).Select(c => c.Content).ToList();
                return headerRow.Any(h => h.Contains("Subject", StringComparison.OrdinalIgnoreCase))
                    && headerRow.Any(h => h.Contains("Marks Obtained", StringComparison.OrdinalIgnoreCase));
            });

            if (subjectTable is null) return new List<ExtractedSubjectDto>();

            var columnIndex = MapHeaderColumns(subjectTable);
            var subjects = new List<ExtractedSubjectDto>();

            var dataRowIndices = subjectTable.Cells
                .Select(c => c.RowIndex)
                .Where(r => r > 0)
                .Distinct()
                .OrderBy(r => r);

            foreach (var rowIndex in dataRowIndices)
            {
                var cellsInRow = subjectTable.Cells.Where(c => c.RowIndex == rowIndex).ToList();
                string? GetCell(string column) =>
                    columnIndex.TryGetValue(column, out var colIdx)
                        ? cellsInRow.FirstOrDefault(c => c.ColumnIndex == colIdx)?.Content?.Trim()
                        : null;

                var subjectName = GetCell("Subject");

                if (string.IsNullOrWhiteSpace(subjectName) ||
                    subjectName.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var gradeRaw = GetCell("Grade");
                var (numericMark, rawMarkSource) = ResolveNumericMark(GetCell);
                var hasNumericMark = numericMark.HasValue;

                subjects.Add(new ExtractedSubjectDto
                {
                    ExtractionSubjectId = Guid.NewGuid(),
                    SubjectName = subjectName,
                    RawMark = hasNumericMark ? rawMarkSource : gradeRaw,
                    NumericMark = numericMark,
                    Symbol = gradeRaw,
                    MarkType = hasNumericMark ? "Percentage" : "Symbol"
                });

                var addSubject = _mapper.Map<ExtractedSubject>(subjects);

                await _unit.SubjectExtraction.AddAsync(addSubject);
            }

            await _unit.SaveChangesAsync();

            return subjects;
        }

        private static (int? NumericMark, string? RawMarkSource) ResolveNumericMark(Func<string, string?> getCell)
        {
            var percentageRaw = getCell("Percentage")?.TrimEnd('%');
            if (decimal.TryParse(percentageRaw, out var percentageValue))
            {
                return ((int)Math.Round(percentageValue), getCell("Percentage"));
            }

            var marksObtainedRaw = getCell("Marks Obtained");
            if (decimal.TryParse(marksObtainedRaw, out var marksObtainedValue))
            {
                var maxMarksRaw = getCell("Max Marks");
                if (decimal.TryParse(maxMarksRaw, out var maxMarksValue) && maxMarksValue > 0 && maxMarksValue != 100)
                {
                    var normalized = (marksObtainedValue / maxMarksValue) * 100m;
                    return ((int)Math.Round(normalized), marksObtainedRaw);
                }

                return ((int)Math.Round(marksObtainedValue), marksObtainedRaw);
            }

            return (null, null);
        }

        private Dictionary<string, int> MapHeaderColumns(DocumentTable subjectTable)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var expectedHeaders = new[]
            {
                "Subject", "Max Marks", "Maximum Marks", "Marks Obtained", "Percentage", "Grade Point", "Grade"
            };

            foreach (var cell in subjectTable.Cells.Where(c => c.RowIndex == 0))
            {
                var match = expectedHeaders.FirstOrDefault(h =>
                    cell.Content.Contains(h, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var canonical = match.Equals("Maximum Marks", StringComparison.OrdinalIgnoreCase)
                        ? "Max Marks"
                        : match;

                    map[canonical] = cell.ColumnIndex;
                }
            }

            return map;
        }

        private void ExtractFieldsFromParagraphs(AnalyzeResult result, ExtractedAcademicRecordDto record)
        {
            var fields = SliceKnownFields(result.Content);

            record.InstitutionName = fields.GetValueOrDefault("School");
            record.StudentName = fields.GetValueOrDefault("StudentName");
            record.StudyLevel = ExtractStudyLevel(result.Content);
            record.InstitutionType = DetermineInstitutionType(record.StudyLevel);
        }

        private static Dictionary<string, string> SliceKnownFields(string rawText)
        {
            var found = new List<(string Canonical, int Index, int Length)>();

            foreach (var (canonical, variants) in FieldLabelVariants)
            {
                var bestIndex = -1;
                var bestLength = 0;

                foreach (var variant in variants)
                {
                    var idx = rawText.IndexOf(variant, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0 && (bestIndex == -1 || idx < bestIndex))
                    {
                        bestIndex = idx;
                        bestLength = variant.Length;
                    }
                }

                if (bestIndex >= 0)
                {
                    found.Add((canonical, bestIndex, bestLength));
                }
            }

            found = found.OrderBy(f => f.Index).ToList();

            var result = new Dictionary<string, string>();

            for (var i = 0; i < found.Count; i++)
            {
                var (canonical, idx, len) = found[i];
                var valueStart = idx + len;
                var valueEnd = i + 1 < found.Count ? found[i + 1].Index : rawText.Length;

                if (valueEnd <= valueStart)
                {
                    continue;
                }

                result[canonical] = rawText.Substring(valueStart, valueEnd - valueStart).Trim(' ', ':', '\r', '\n');
            }

            return result;
        }

        private string? ExtractStudyLevel(string rawText)
        {
            var patterns = new[]
            {
                @"Grade\s+\d{1,2}",
                @"N[2-6]",
                @"NCV\s+Level\s+\d",
                @"\d(?:st|nd|rd|th)\s+year",
                @"Semester\s+\d",
                @"Year\s+\d"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(rawText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Value;
                }
            }

            return null;
        }

        private string? DetermineInstitutionType(string? studyLevel)
        {
            if (studyLevel == null)
            {
                return "Unknown";
            }

            if (Regex.IsMatch(studyLevel, @"Grade", RegexOptions.IgnoreCase))
            {
                return "High School";
            }

            if (Regex.IsMatch(studyLevel, @"N[2-6]|NCV", RegexOptions.IgnoreCase))
            {
                return "TVET College";
            }

            if (Regex.IsMatch(studyLevel, @"Year|Semester", RegexOptions.IgnoreCase))
            {
                return "University";
            }

            return "Unknown";
        }
    }
}