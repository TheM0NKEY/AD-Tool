using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep1InputViewModelTests
{
    [Fact]
    public void InputTable_HasUPNColumnOnCreation()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        Assert.True(vm.InputTable.Columns.Contains("UPN"));
    }

    [Fact]
    public void NextCommand_DisabledWhenNoRows()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void AddRow_AddsRowToInputTable()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        vm.AddRowCommand.Execute(null);
        Assert.Equal(1, vm.InputTable.Rows.Count);
    }

    [Fact]
    public void AddUsersFromBrowser_AddsRowsWithUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var users = new List<AdUser> { new("alice@contoso.com", "Alice Smith") };

        vm.AddUsersFromBrowser(users);

        Assert.Equal(1, vm.InputTable.Rows.Count);
        Assert.Equal("alice@contoso.com", vm.InputTable.Rows[0]["UPN"]);
    }

    [Fact]
    public void AddUsersFromBrowser_SkipsDuplicateUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var users = new List<AdUser>
        {
            new("alice@contoso.com", "Alice"),
            new("alice@contoso.com", "Alice Duplicate")
        };

        vm.AddUsersFromBrowser(users);

        Assert.Equal(1, vm.InputTable.Rows.Count);
    }

    [Fact]
    public void Next_PopulatesEntriesFromInputTable()
    {
        bool nextCalled = false;
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => nextCalled = true);

        // Add a column and populate a row directly
        vm.InputTable.Columns.Add("department", typeof(string));
        var row = vm.InputTable.NewRow();
        row["UPN"] = "alice@contoso.com";
        row["department"] = "Engineering";
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.True(nextCalled);
        Assert.Single(entries);
        Assert.Equal("alice@contoso.com", entries[0].UserUPN);
        Assert.Equal("Engineering", entries[0].Attributes["department"]);
    }

    [Fact]
    public void Next_SkipsRowsWithBlankUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var row = vm.InputTable.NewRow();
        row["UPN"] = "";
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.Empty(entries);
    }

    [Fact]
    public void Next_SkipsBlankAttributeValues()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        vm.InputTable.Columns.Add("department", typeof(string));
        vm.InputTable.Columns.Add("title", typeof(string));
        var row = vm.InputTable.NewRow();
        row["UPN"] = "alice@contoso.com";
        row["department"] = "IT";
        row["title"] = ""; // blank — should be omitted
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.True(entries[0].Attributes.ContainsKey("department"));
        Assert.False(entries[0].Attributes.ContainsKey("title"));
    }
}
