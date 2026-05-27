using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep4ExecuteViewModelTests
{
    [Fact]
    public async Task ExecuteAllAsync_SuccessfulUpdate_SetsSuccessStatus()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateAttributesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
              .ReturnsAsync(new ExecutionResult(true));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", Attributes = { ["department"] = "IT" } }
        };
        var vm = new AttrStep4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.Equal(ExecutionStatus.Success, entries[0].ExecutionStatus);
        Assert.Equal(1, vm.SuccessCount);
        Assert.Equal(0, vm.FailCount);
    }

    [Fact]
    public async Task ExecuteAllAsync_FailedUpdate_SetsFailedStatusAndMessages()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateAttributesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
              .ReturnsAsync(new ExecutionResult(false, ExecutionErrorType.UnexpectedError, "AD error"));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", Attributes = { ["department"] = "IT" } }
        };
        var vm = new AttrStep4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.Equal(ExecutionStatus.Failed, entries[0].ExecutionStatus);
        Assert.Equal(1, vm.FailCount);
        Assert.NotNull(entries[0].ErrorTitle);
        Assert.NotNull(entries[0].ErrorDetail);
    }

    [Fact]
    public async Task ExecuteAllAsync_BlankAttributesNotPassedToService()
    {
        Dictionary<string, string>? capturedAttrs = null;
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.UpdateAttributesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
              .Callback<string, Dictionary<string, string>>((_, attrs) => capturedAttrs = attrs)
              .ReturnsAsync(new ExecutionResult(true));
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com",
                    Attributes = { ["department"] = "IT", ["title"] = "" } }
        };
        var vm = new AttrStep4ExecuteViewModel(entries, adMock.Object, () => { });

        await vm.ExecuteAllAsync();

        Assert.NotNull(capturedAttrs);
        Assert.True(capturedAttrs!.ContainsKey("department"));
        Assert.False(capturedAttrs!.ContainsKey("title"));
    }

    [Fact]
    public void StartNewRunCommand_CallsOnReset()
    {
        bool resetCalled = false;
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep4ExecuteViewModel(entries, new Mock<IAdService>().Object, () => resetCalled = true);

        vm.StartNewRunCommand.Execute(null);

        Assert.True(resetCalled);
    }
}
