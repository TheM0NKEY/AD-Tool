using ADTool.Services;
using ADTool.ViewModels;

namespace ADTool.Tests;

public class AttributeToolViewModelTests
{
    [Fact]
    public void InitialStep_IsAttrStep1InputViewModel()
    {
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { });
        Assert.IsType<AttrStep1InputViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void GoTo_ChangesCurrentStep()
    {
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { });
        vm.GoTo(2);
        Assert.IsType<AttrStep2ValidateViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void GoTo_InvalidStep_Throws()
    {
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => { });
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => vm.GoTo(0)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => vm.GoTo(5)));
    }

    [Fact]
    public void StartNewRun_CallsReturnHome()
    {
        bool returned = false;
        var vm = new AttributeToolViewModel(new AdServiceStub(), () => returned = true);
        vm.GoTo(4);
        var step4 = (AttrStep4ExecuteViewModel)vm.CurrentStep;
        step4.StartNewRunCommand.Execute(null);
        Assert.True(returned);
    }
}
