using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class Step2ValidateViewModelTests
{
    private static ObservableCollection<UPNChangeEntry> TwoEntries() => new(
    [
        new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "a@new.com" },
        new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "b@new.com" }
    ]);

    [Fact]
    public async Task ValidateAllAsync_SetsValidStatusForFoundUsers()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Display Name"));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.Valid, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SetsNotFoundStatus()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.NotFound, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SetsErrorTitleAndDetail()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "missing@old.com", NewUPN = "missing@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.Equal("User not found", entries[0].ErrorTitle);
        Assert.NotNull(entries[0].ErrorDetail);
    }

    [Fact]
    public async Task NextCommand_DisabledWhenInvalidRowsExist()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public async Task RemoveInvalidRows_RemovesOnlyInvalidEntries()
    {
        var adMock = new Mock<IAdService>();
        adMock.SetupSequence(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Valid User"))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();
        vm.RemoveInvalidRowsCommand.Execute(null);

        Assert.Single(entries);
        Assert.Equal(ValidationStatus.Valid, entries[0].ValidationStatus);
    }

    [Fact]
    public async Task ValidateAllAsync_SameBatchDuplicateNewUPN_BothMarkedDuplicate()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Display Name"));
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "shared@new.com" },
            new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "shared@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.DuplicateNewUPN, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SameBatchDuplicateNewUPN_SetsErrorMessages()
    {
        var adMock = new Mock<IAdService>();
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "shared@new.com" },
            new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "shared@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.NotNull(e.ErrorTitle));
        Assert.All(entries, e => Assert.NotNull(e.ErrorDetail));
    }

    [Fact]
    public async Task ValidateAllAsync_SameBatchDuplicateNewUPN_CaseInsensitive()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Display Name"));
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "Shared@New.com" },
            new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "shared@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.DuplicateNewUPN, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_MixedBatch_OnlyDuplicatesAreBlocked()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Display Name"));
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "shared@new.com" },
            new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "shared@new.com" },
            new UPNChangeEntry { OldUPN = "c@old.com", NewUPN = "unique@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.Equal(ValidationStatus.DuplicateNewUPN, entries[0].ValidationStatus);
        Assert.Equal(ValidationStatus.DuplicateNewUPN, entries[1].ValidationStatus);
        Assert.Equal(ValidationStatus.Valid, entries[2].ValidationStatus);
    }
}
