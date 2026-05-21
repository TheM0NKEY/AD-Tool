using ADTool.Services;
using ADTool.ViewModels;
using Moq;

namespace ADTool.Tests;

public class MainViewModelTests
{
    private readonly Mock<IAdService> _adMock = new();
    private readonly CsvImportService _csvSvc = new();

    [Fact]
    public void InitialStep_IsStep1InputViewModel()
    {
        var vm = new MainViewModel(_adMock.Object, _csvSvc);
        Assert.IsType<Step1InputViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void AfterReset_CurrentStepIsStep1AndEntriesCleared()
    {
        var vm = new MainViewModel(_adMock.Object, _csvSvc);
        vm.Entries.Add(new ADTool.Models.UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com" });

        vm.ResetCommand.Execute(null);

        Assert.IsType<Step1InputViewModel>(vm.CurrentStep);
        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void GoTo_ChangesCurrentStep()
    {
        var vm = new MainViewModel(_adMock.Object, _csvSvc);
        vm.GoTo(2);
        Assert.IsType<Step2ValidateViewModel>(vm.CurrentStep);
    }

    [Fact]
    public void GoTo_InvalidStep_Throws()
    {
        var vm = new MainViewModel(_adMock.Object, _csvSvc);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(5));
    }
}
