using ADTool.Services;
using ADTool.ViewModels;

namespace ADTool.Tests;

public class UPNToolViewModelTests
{
    [Fact]
    public void InitialStep_IsStep1InputViewModel()
    {
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { });
        Assert.IsType<Step1InputViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void GoTo_ChangesCurrentStep()
    {
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { });
        vm.GoTo(2);
        Assert.IsType<Step2ValidateViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void GoTo_InvalidStep_Throws()
    {
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => { });
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(5));
    }

    [Fact]
    public void StartNewRun_CallsReturnHome()
    {
        bool returned = false;
        var vm = new UPNToolViewModel(new AdServiceStub(), new CsvImportService(), () => returned = true);
        vm.GoTo(4);
        var step4 = (Step4ExecuteViewModel)vm.CurrentStep;
        step4.StartNewRunCommand.Execute(null);
        Assert.True(returned);
    }
}
