using ADTool.Services;
using ADTool.ViewModels;
using Moq;

namespace ADTool.Tests;

public class AppShellViewModelTests
{
    private readonly Mock<IAdService> _adMock = new();
    private readonly CsvImportService _csvSvc = new();

    [Fact]
    public void InitialView_IsHomeViewModel()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        Assert.IsType<HomeViewModel>(vm.CurrentView);
    }

    [Fact]
    public void LaunchUPNModifier_SetsCurrentViewToUPNToolViewModel()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchUPNModifierCommand.Execute(null);
        Assert.IsType<UPNToolViewModel>(vm.CurrentView);
    }

    [Fact]
    public void ReturnHome_SetsCurrentViewToHomeViewModel()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchUPNModifierCommand.Execute(null);
        vm.ReturnHome();
        Assert.IsType<HomeViewModel>(vm.CurrentView);
    }

    [Fact]
    public void WindowTitle_IsADTool_OnHomeScreen()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        Assert.Equal("AD Tool", vm.WindowTitle);
    }

    [Fact]
    public void WindowTitle_IsUPNModifier_WhenUPNToolActive()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchUPNModifierCommand.Execute(null);
        Assert.Equal("AD Tool — UPN Modifier", vm.WindowTitle);
    }

    [Fact]
    public void WindowTitle_ReturnsToADTool_AfterReturnHome()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchUPNModifierCommand.Execute(null);
        vm.ReturnHome();
        Assert.Equal("AD Tool", vm.WindowTitle);
    }
}
