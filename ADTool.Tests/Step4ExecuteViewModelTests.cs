using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class Step4ExecuteViewModelTests
{
    private static ObservableCollection<UPNChangeEntry> TwoEntries() => new(
    [
        new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "a@new.com", ValidationStatus = ValidationStatus.Valid },
        new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "b@new.com", ValidationStatus = ValidationStatus.Valid }
    ]);

    [Fact]
    public async Task ExecuteAllAsync_SetsSuccessStatus_WhenServiceSucceeds()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ExecutionResult(true));
        var entries = TwoEntries();
        var vm = new Step4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.All(entries, e => Assert.Equal(ExecutionStatus.Success, e.ExecutionStatus));
        Assert.Equal(2, vm.SuccessCount);
        Assert.Equal(0, vm.FailCount);
    }

    [Fact]
    public async Task ExecuteAllAsync_SetsFailedStatusAndErrorMessage()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ExecutionResult(false, ExecutionErrorType.InsufficientPermissions, "Access denied"));
        var entries = TwoEntries();
        var vm = new Step4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.All(entries, e => Assert.Equal(ExecutionStatus.Failed, e.ExecutionStatus));
        Assert.Equal("Insufficient permissions", entries[0].ErrorTitle);
        Assert.NotNull(entries[0].ErrorDetail);
        Assert.Equal(0, vm.SuccessCount);
        Assert.Equal(2, vm.FailCount);
    }

    [Fact]
    public async Task ExecuteAllAsync_RunsSequentially_CallsServiceForEachEntry()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ExecutionResult(true));
        var entries = TwoEntries();
        var vm = new Step4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        adMock.Verify(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAllAsync_Sets_ErrorInfo_For_ProxyAddressConflict()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ExecutionResult(false, ExecutionErrorType.ProxyAddressConflict, null));
        var entries = TwoEntries();
        var vm = new Step4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.All(entries, e =>
        {
            Assert.False(string.IsNullOrEmpty(e.ErrorTitle));
            Assert.False(string.IsNullOrEmpty(e.ErrorDetail));
        });
    }

    [Fact]
    public async Task ExecuteAllAsync_Sets_ErrorInfo_For_UnexpectedError()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ExecutionResult(false, ExecutionErrorType.UnexpectedError, "some technical detail"));
        var entries = TwoEntries();
        var vm = new Step4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.All(entries, e =>
        {
            Assert.False(string.IsNullOrEmpty(e.ErrorTitle));
            Assert.False(string.IsNullOrEmpty(e.ErrorDetail));
        });
    }
}
