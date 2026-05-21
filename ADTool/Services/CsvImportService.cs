using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace ADTool.Services;

public record CsvImportResult(IReadOnlyList<(string OldUPN, string NewUPN)> Rows, IReadOnlyList<string> Errors);

public class CsvImportService
{
    public CsvImportResult Import(string filePath, IEnumerable<string> existingOldUpns)
    {
        var rows = new List<(string OldUPN, string NewUPN)>();
        var errors = new List<string>();
        var existingSet = new HashSet<string>(existingOldUpns, StringComparer.OrdinalIgnoreCase);

        try
        {
            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord ?? [];
            string? oldHeader = headers.FirstOrDefault(h => h.Equals("OldUPN", StringComparison.OrdinalIgnoreCase));
            string? newHeader = headers.FirstOrDefault(h => h.Equals("NewUPN", StringComparison.OrdinalIgnoreCase));

            if (oldHeader is null || newHeader is null)
            {
                errors.Add("CSV must contain columns 'OldUPN' and 'NewUPN'.");
                return new CsvImportResult(rows, errors);
            }

            var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int rowNum = 1;

            while (csv.Read())
            {
                rowNum++;
                string oldUpn = csv.GetField<string>(oldHeader)?.Trim() ?? string.Empty;
                string newUpn = csv.GetField<string>(newHeader)?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(oldUpn) || string.IsNullOrEmpty(newUpn))
                {
                    errors.Add($"Row {rowNum}: OldUPN and NewUPN cannot be blank.");
                    continue;
                }
                if (seenInBatch.Contains(oldUpn))
                {
                    errors.Add($"Row {rowNum}: Duplicate OldUPN '{oldUpn}' within import file.");
                    continue;
                }
                if (existingSet.Contains(oldUpn))
                {
                    errors.Add($"Row {rowNum}: OldUPN '{oldUpn}' already exists in the current list.");
                    continue;
                }

                seenInBatch.Add(oldUpn);
                rows.Add((oldUpn, newUpn));
            }
        }
        catch (FileNotFoundException)
        {
            errors.Add($"File not found: {filePath}");
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read CSV: {ex.Message}");
        }

        return new CsvImportResult(rows, errors);
    }
}
