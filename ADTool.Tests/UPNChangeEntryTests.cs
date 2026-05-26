using ADTool.Models;
using System.ComponentModel;
using Xunit;

namespace ADTool.Tests;

public class UPNChangeEntryTests
{
    [Fact]
    public void PropertyChanged_FiresWhenOldUPNSet()
    {
        var entry = new UPNChangeEntry();
        string? changedProp = null;
        entry.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        entry.OldUPN = "jsmith@old.com";

        Assert.Equal(nameof(UPNChangeEntry.OldUPN), changedProp);
    }

    [Fact]
    public void PropertyChanged_FiresWhenNewUPNSet()
    {
        var entry = new UPNChangeEntry();
        var fired = new List<string?>();
        entry.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        entry.NewUPN = "jsmith@new.com";

        Assert.Contains(nameof(UPNChangeEntry.NewUPN), fired);
    }

    [Fact]
    public void PropertyChanged_FiresWhenValidationStatusSet()
    {
        var entry = new UPNChangeEntry();
        string? changedProp = null;
        entry.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        entry.ValidationStatus = ValidationStatus.Valid;

        Assert.Equal(nameof(UPNChangeEntry.ValidationStatus), changedProp);
    }

    [Fact]
    public void DefaultValidationStatus_IsPending()
    {
        var entry = new UPNChangeEntry();
        Assert.Equal(ValidationStatus.Pending, entry.ValidationStatus);
    }

    [Fact]
    public void DefaultExecutionStatus_IsPending()
    {
        var entry = new UPNChangeEntry();
        Assert.Equal(ExecutionStatus.Pending, entry.ExecutionStatus);
    }

    [Fact]
    public void MailNickname_ReturnsUpnPrefix()
    {
        var entry = new UPNChangeEntry { NewUPN = "alice@contoso.com" };
        Assert.Equal("alice", entry.MailNickname);
    }

    [Fact]
    public void MailNickname_NoAtSign_ReturnsFullValue()
    {
        var entry = new UPNChangeEntry { NewUPN = "alice" };
        Assert.Equal("alice", entry.MailNickname);
    }

    [Fact]
    public void SettingNewUpn_FiresPropertyChangedForMailNickname()
    {
        var entry = new UPNChangeEntry();
        var fired = new List<string?>();
        entry.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        entry.NewUPN = "bob@contoso.com";

        Assert.Contains(nameof(UPNChangeEntry.MailNickname), fired);
    }
}
