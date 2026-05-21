using ADTool.Services;
using System.IO;

namespace ADTool.Tests;

public class CsvImportServiceTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();
    private readonly CsvImportService _svc = new();

    public void Dispose() => File.Delete(_tempFile);

    private void Write(string content) => File.WriteAllText(_tempFile, content);

    [Fact]
    public void Import_ValidCsv_ReturnsRows()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com\nawhite@old.com,awhite@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Equal(2, result.Rows.Count);
        Assert.Empty(result.Errors);
        Assert.Equal("jsmith@old.com", result.Rows[0].OldUPN);
        Assert.Equal("jsmith@new.com", result.Rows[0].NewUPN);
    }

    [Fact]
    public void Import_MissingOldUPNColumn_ReturnsError()
    {
        Write("Source,NewUPN\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("OldUPN", result.Errors[0]);
    }

    [Fact]
    public void Import_BlankField_SkipsRowAndReportsError()
    {
        Write("OldUPN,NewUPN\n,jsmith@new.com\nawhite@old.com,awhite@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Import_DuplicateInFile_SkipsSecondAndReportsError()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com\njsmith@old.com,jsmith@new2.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate", result.Errors[0]);
    }

    [Fact]
    public void Import_DuplicateAgainstExisting_SkipsAndReportsError()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, ["jsmith@old.com"]);
        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public void Import_HeadersCaseInsensitive_Works()
    {
        Write("oldupn,newupn\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Empty(result.Errors);
    }
}
