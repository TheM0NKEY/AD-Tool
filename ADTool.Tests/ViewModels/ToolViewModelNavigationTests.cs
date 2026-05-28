using ADTool.Services;
using ADTool.ViewModels;
using Xunit;

namespace ADTool.Tests.ViewModels;

public class ToolViewModelNavigationTests
{
    [Fact]
    public void UPN_CurrentStepNumber_StartsAtOne()
    {
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { });
        Assert.Equal(1, vm.CurrentStepNumber);
    }

    [Fact]
    public void UPN_CurrentStepNumber_UpdatesOnGoTo()
    {
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { });
        vm.GoTo(3);
        Assert.Equal(3, vm.CurrentStepNumber);
    }

    [Fact]
    public void UPN_ReturnHomeCommand_InvokesCallback()
    {
        bool called = false;
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { called = true; });
        vm.ReturnHomeCommand.Execute(null);
        Assert.True(called);
    }

    [Fact]
    public void Attr_CurrentStepNumber_StartsAtOne()
    {
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { });
        Assert.Equal(1, vm.CurrentStepNumber);
    }

    [Fact]
    public void Attr_CurrentStepNumber_UpdatesOnGoTo()
    {
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { });
        vm.GoTo(2);
        Assert.Equal(2, vm.CurrentStepNumber);
    }

    [Fact]
    public void Attr_ReturnHomeCommand_InvokesCallback()
    {
        bool called = false;
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { called = true; });
        vm.ReturnHomeCommand.Execute(null);
        Assert.True(called);
    }
}
