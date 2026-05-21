using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class Step1InputViewModelTests
{
    private ObservableCollection<UPNChangeEntry> Entries() => new();
    private CsvImportService Csv() => new();

    [Fact]
    public void NextCommand_DisabledWhenEntriesEmpty()
    {
        var vm = new Step1InputViewModel(Entries(), Csv(), () => { });
        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void NextCommand_EnabledWhenEntriesHasItems()
    {
        var entries = Entries();
        var vm = new Step1InputViewModel(entries, Csv(), () => { });
        entries.Add(new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com" });
        Assert.True(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void NextCommand_ResetsPendingValidationStatus()
    {
        var entries = Entries();
        entries.Add(new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com", ValidationStatus = ValidationStatus.Valid });
        bool nextCalled = false;
        var vm = new Step1InputViewModel(entries, Csv(), () => nextCalled = true);

        vm.NextCommand.Execute(null);

        Assert.Equal(ValidationStatus.Pending, entries[0].ValidationStatus);
        Assert.True(nextCalled);
    }

    [Fact]
    public void ApplySuffixSwap_ReplacesMatchingSuffix()
    {
        var entries = Entries();
        entries.Add(new UPNChangeEntry { OldUPN = "jsmith@old.com", NewUPN = "jsmith@old.com" });
        var vm = new Step1InputViewModel(entries, Csv(), () => { });
        vm.OldSuffix = "@old.com";
        vm.NewSuffix = "@new.com";

        vm.ApplySuffixSwapCommand.Execute(null);

        Assert.Equal("jsmith@new.com", entries[0].NewUPN);
    }

    [Fact]
    public void ApplySuffixSwap_DisabledWhenSuffixesEmpty()
    {
        var vm = new Step1InputViewModel(Entries(), Csv(), () => { });
        Assert.False(vm.ApplySuffixSwapCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteRowCommand_RemovesEntry()
    {
        var entries = Entries();
        var entry = new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com" };
        entries.Add(entry);
        var vm = new Step1InputViewModel(entries, Csv(), () => { });

        vm.DeleteRowCommand.Execute(entry);

        Assert.Empty(entries);
    }

    [Fact]
    public void AddRowCommand_AddsBlankEntry()
    {
        var entries = Entries();
        var vm = new Step1InputViewModel(entries, Csv(), () => { });

        vm.AddRowCommand.Execute(null);

        Assert.Single(entries);
    }

    [Fact]
    public void ApplySuffixSwap_LeavesNonMatchingEntriesUnchanged()
    {
        var entries = Entries();
        entries.Add(new UPNChangeEntry { OldUPN = "user@wrong.com", NewUPN = "user@wrong.com" });
        var vm = new Step1InputViewModel(entries, Csv(), () => { });
        vm.OldSuffix = "@old.com";
        vm.NewSuffix = "@new.com";

        vm.ApplySuffixSwapCommand.Execute(null);

        Assert.Equal("user@wrong.com", entries[0].NewUPN);
    }
}
