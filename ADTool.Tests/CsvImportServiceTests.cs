using ADTool.Services;
using System.IO;
using Xunit;

namespace ADTool.Tests;

public class CsvImportServiceTests
{
    private static string WriteTempCsv(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
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
