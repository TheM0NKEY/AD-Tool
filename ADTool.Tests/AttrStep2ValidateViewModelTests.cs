using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep2ValidateViewModelTests
{
    [Fact]
    public async Task ValidateAllAsync_UserExists_SetsValidStatus()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Alice Smith"));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.Equal(ValidationStatus.Valid, entries[0].ValidationStatus);
        Assert.Equal("Alice Smith", entries[0].DisplayName);
    }

    [Fact]
    public async Task ValidateAllAsync_UserNotFound_SetsNotFoundStatus()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "missing@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.Equal(ValidationStatus.NotFound, entries[0].ValidationStatus);
        Assert.NotNull(entries[0].ErrorTitle);
        Assert.NotNull(entries[0].ErrorDetail);
    }

    [Fact]
    public async Task ValidateAllAsync_SameBatchDuplicateUPN_BothMarkedDuplicate()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "User"));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com" },
            new() { UserUPN = "alice@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.DuplicateNewUPN, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SameBatchDuplicate_CaseInsensitive()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "User"));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "Alice@Contoso.com" },
            new() { UserUPN = "alice@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.DuplicateNewUPN, e.ValidationStatus));
    }

    [Fact]
    public async Task RemoveInvalidRows_RemovesOnlyInvalidEntries()
    {
        var adMock = new Mock<IAdService>();
        adMock.SetupSequence(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Valid User"))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@contoso.com" },
            new() { UserUPN = "b@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();
        vm.RemoveInvalidRowsCommand.Execute(null);

        Assert.Single(entries);
        Assert.Equal(ValidationStatus.Valid, entries[0].ValidationStatus);
    }

    [Fact]
    public async Task NextCommand_DisabledWhenInvalidRowsExist()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@contoso.com" }
        };
        var vm = new AttrStep2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.False(vm.NextCommand.CanExecute(null));
    }
}
