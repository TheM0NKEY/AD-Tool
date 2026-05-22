using ADTool.Services;
using System.IO;

namespace ADTool.Tests;

public class CsvImportServiceTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    private string WriteTempCsv(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Import_ValidCsv_ReturnsRows()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN,NewUPN\nalice@old.com,alice@new.com\nbob@old.com,bob@new.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@old.com", result.Rows[0].OldUPN);
        Assert.Equal("alice@new.com", result.Rows[0].NewUPN);
        Assert.Equal("bob@old.com", result.Rows[1].OldUPN);
        Assert.Equal("bob@new.com", result.Rows[1].NewUPN);
    }

    [Fact]
    public void Import_DuplicateInFile_SkipsSecondAndReportsError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN\nalice@old.com\nalice@old.com\nbob@old.com");
        var result = svc.Import(path, []);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate", result.Errors[0]);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@old.com", result.Rows[0].OldUPN);
        Assert.Equal("bob@old.com", result.Rows[1].OldUPN);
    }

    [Fact]
    public void Import_DuplicateAgainstExisting_SkipsAndReportsError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN\nalice@old.com\nbob@old.com");
        var existing = new[] { "alice@old.com" };
        var result = svc.Import(path, existing);
        Assert.Single(result.Errors);
        Assert.Contains("already exists", result.Errors[0]);
        Assert.Single(result.Rows);
        Assert.Equal("bob@old.com", result.Rows[0].OldUPN);
    }

    [Fact]
    public void Import_HeadersCaseInsensitive_Works()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("oldupn,NEWUPN\nalice@old.com,alice@new.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Single(result.Rows);
        Assert.Equal("alice@old.com", result.Rows[0].OldUPN);
        Assert.Equal("alice@new.com", result.Rows[0].NewUPN);
    }

    [Fact]
    public void Import_OldUpnOnly_ReturnsRowsWithEmptyNewUpn()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN\nalice@old.com\nbob@old.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@old.com", result.Rows[0].OldUPN);
        Assert.Equal("", result.Rows[0].NewUPN);
        Assert.Equal("bob@old.com", result.Rows[1].OldUPN);
        Assert.Equal("", result.Rows[1].NewUPN);
    }

    [Fact]
    public void Import_NewUpnColumnPresentButBlank_SkipsRowWithError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN,NewUPN\nalice@old.com,\nbob@old.com,bob@new.com");
        var result = svc.Import(path, []);
        Assert.Single(result.Errors);
        Assert.Single(result.Rows);
        Assert.Equal("bob@old.com", result.Rows[0].OldUPN);
        Assert.Equal("bob@new.com", result.Rows[0].NewUPN);
    }

    [Fact]
    public void Import_MissingOldUpnColumn_ReturnsFileError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("NewUPN\nbob@new.com");
        var result = svc.Import(path, []);
        Assert.Single(result.Errors);
        Assert.Contains("OldUPN", result.Errors[0]);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_BothColumns_PreservesExistingBehavior()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN,NewUPN\nalice@old.com,alice@new.com\nbob@old.com,bob@new.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@new.com", result.Rows[0].NewUPN);
    }
}
