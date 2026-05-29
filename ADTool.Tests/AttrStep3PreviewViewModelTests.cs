using ADTool.Models;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep3PreviewViewModelTests
{
    [Fact]
    public void PreviewTable_HasDisplayNameAndUPNColumns()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", DisplayName = "Alice",
                    Attributes = { ["department"] = "IT" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        vm.Refresh();
        Assert.True(vm.PreviewTable.Columns.Contains("Display Name"));
        Assert.True(vm.PreviewTable.Columns.Contains("UPN"));
    }

    [Fact]
    public void PreviewTable_HasColumnForEachAttributeKey()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com",
                    Attributes = { ["department"] = "IT", ["title"] = "Dev" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        vm.Refresh();
        Assert.True(vm.PreviewTable.Columns.Contains("department"));
        Assert.True(vm.PreviewTable.Columns.Contains("title"));
    }

    [Fact]
    public void PreviewTable_RowCountMatchesEntryCount()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@contoso.com", Attributes = { ["department"] = "IT" } },
            new() { UserUPN = "b@contoso.com", Attributes = { ["department"] = "HR" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        vm.Refresh();
        Assert.Equal(2, vm.PreviewTable.Rows.Count);
    }

    [Fact]
    public void PreviewTable_RowValuesMatchEntryData()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", DisplayName = "Alice Smith",
                    Attributes = { ["department"] = "Engineering" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        vm.Refresh();
        var row = vm.PreviewTable.Rows[0];
        Assert.Equal("alice@contoso.com", row["UPN"]);
        Assert.Equal("Alice Smith",       row["Display Name"]);
        Assert.Equal("Engineering",       row["department"]);
    }

    [Fact]
    public void PreviewTable_MissingAttributeInRow_ShowsEmpty()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@c.com", Attributes = { ["department"] = "IT" } },
            new() { UserUPN = "b@c.com", Attributes = { /* no department */ } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        vm.Refresh();
        Assert.Equal("", vm.PreviewTable.Rows[1]["department"]);
    }
}
