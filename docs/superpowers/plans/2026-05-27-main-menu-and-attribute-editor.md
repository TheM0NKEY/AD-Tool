# Main Menu and Bulk Attribute Editor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a home-screen launcher and a new 4-step Attribute Editor tool that bulk-sets AD attributes (Department, Title, extensionAttribute1–15, etc.) for a list of users.

**Architecture:** `MainViewModel` is replaced by `AppShellViewModel` (thin shell holding `CurrentView`). A new `HomeView` launches two tools: the existing UPN Modifier (now wrapped in `UPNToolViewModel`) and a new `AttributeToolViewModel` with its own 4-step flow. The attribute editor uses a `DataTable`-backed grid so columns are dynamic. `IAdService` gains `ValidateUserExistsAsync` and `UpdateAttributesAsync`.

**Tech Stack:** C# / WPF .NET 8, `System.Data.DataTable`, CsvHelper, xunit, Moq

---

## Files

| File | Change |
|------|--------|
| `ADTool/ViewModels/AppShellViewModel.cs` | New — replaces MainViewModel |
| `ADTool/ViewModels/HomeViewModel.cs` | New |
| `ADTool/ViewModels/UPNToolViewModel.cs` | New — thin wrapper around existing 4 UPN steps |
| `ADTool/ViewModels/MainViewModel.cs` | Delete |
| `ADTool/Views/HomeView.xaml` + `.cs` | New |
| `ADTool/Views/UPNToolView.xaml` + `.cs` | New — holds step indicator + step DataTemplates |
| `ADTool/Views/MainWindow.xaml` | Simplify: remove step indicator, title binding, top-level DataTemplates only |
| `ADTool/App.xaml.cs` | Wire AppShellViewModel instead of MainViewModel |
| `ADTool.Tests/AppShellViewModelTests.cs` | New |
| `ADTool.Tests/UPNToolViewModelTests.cs` | New |
| `ADTool.Tests/MainViewModelTests.cs` | Delete |
| `ADTool/Models/AttributeChangeEntry.cs` | New |
| `ADTool/Models/AttributeColumnMap.cs` | New |
| `ADTool.Tests/AttributeColumnMapTests.cs` | New |
| `ADTool/Services/IAdService.cs` | Add `ValidateUserExistsAsync` + `UpdateAttributesAsync` |
| `ADTool/Services/AdServiceStub.cs` | Implement new methods |
| `ADTool/Services/AdService.cs` | Implement new methods |
| `ADTool/ViewModels/AttrStep2ValidateViewModel.cs` | New |
| `ADTool/Views/AttrStep2ValidateView.xaml` + `.cs` | New |
| `ADTool.Tests/AttrStep2ValidateViewModelTests.cs` | New |
| `ADTool/ViewModels/AttrStep3PreviewViewModel.cs` | New |
| `ADTool/Views/AttrStep3PreviewView.xaml` + `.cs` | New |
| `ADTool.Tests/AttrStep3PreviewViewModelTests.cs` | New |
| `ADTool/ViewModels/AttrStep4ExecuteViewModel.cs` | New |
| `ADTool/Views/AttrStep4ExecuteView.xaml` + `.cs` | New |
| `ADTool.Tests/AttrStep4ExecuteViewModelTests.cs` | New |
| `ADTool/ViewModels/AttrStep1InputViewModel.cs` | New |
| `ADTool/Views/AttrStep1InputView.xaml` + `.cs` | New |
| `ADTool/Views/AddColumnDialog.xaml` + `.cs` | New |
| `ADTool.Tests/AttrStep1InputViewModelTests.cs` | New |
| `ADTool/ViewModels/AttributeToolViewModel.cs` | New |
| `ADTool/Views/AttrToolView.xaml` + `.cs` | New |
| `ADTool.Tests/AttributeToolViewModelTests.cs` | New |
| `README.md` | Add Attribute Editor section |

---

## Task 1: Shell Restructuring — AppShellViewModel, HomeViewModel, UPNToolViewModel

**Files:**
- Create: `ADTool.Tests/AppShellViewModelTests.cs`
- Create: `ADTool.Tests/UPNToolViewModelTests.cs`
- Delete: `ADTool.Tests/MainViewModelTests.cs`
- Create: `ADTool/ViewModels/AppShellViewModel.cs`
- Create: `ADTool/ViewModels/HomeViewModel.cs`
- Create: `ADTool/ViewModels/UPNToolViewModel.cs`
- Delete: `ADTool/ViewModels/MainViewModel.cs`
- Create: `ADTool/Views/HomeView.xaml` + `HomeView.xaml.cs`
- Create: `ADTool/Views/UPNToolView.xaml` + `UPNToolView.xaml.cs`
- Modify: `ADTool/Views/MainWindow.xaml`
- Modify: `ADTool/App.xaml.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AppShellViewModelTests.cs`:

```csharp
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
```

Create `ADTool.Tests/UPNToolViewModelTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to confirm they fail (type not found)**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AppShellViewModelTests|UPNToolViewModelTests"
```

Expected: build error — `AppShellViewModel`, `HomeViewModel`, `UPNToolViewModel` not found.

- [ ] **Step 3: Create AppShellViewModel.cs**

Create `ADTool/ViewModels/AppShellViewModel.cs`:

```csharp
using ADTool.Services;

namespace ADTool.ViewModels;

public class AppShellViewModel : BaseViewModel
{
    private BaseViewModel _currentView;
    private readonly IAdService _adService;
    private readonly CsvImportService _csvService;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set
        {
            SetField(ref _currentView, value);
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public string WindowTitle => CurrentView switch
    {
        UPNToolViewModel       => "AD Tool — UPN Modifier",
        AttributeToolViewModel => "AD Tool — Attribute Editor",
        _                      => "AD Tool"
    };

    public RelayCommand LaunchUPNModifierCommand { get; }
    public RelayCommand LaunchAttributeEditorCommand { get; }

    public AppShellViewModel(IAdService adService, CsvImportService csvService)
    {
        _adService = adService;
        _csvService = csvService;
        LaunchUPNModifierCommand = new RelayCommand(LaunchUPNModifier);
        LaunchAttributeEditorCommand = new RelayCommand(LaunchAttributeEditor);
        _currentView = new HomeViewModel(LaunchUPNModifier, LaunchAttributeEditor);
    }

    public void ReturnHome()
    {
        CurrentView = new HomeViewModel(LaunchUPNModifier, LaunchAttributeEditor);
    }

    private void LaunchUPNModifier()
    {
        CurrentView = new UPNToolViewModel(_adService, _csvService, ReturnHome);
    }

    private void LaunchAttributeEditor()
    {
        // Wired in Task 8 — AttributeToolViewModel not yet available
    }
}
```

Note: `AttributeToolViewModel` is referenced in the switch but doesn't exist yet. The switch compiles fine as a forward reference only because it will be added in Task 8. For now the `LaunchAttributeEditor` method is a no-op.

- [ ] **Step 4: Create HomeViewModel.cs**

Create `ADTool/ViewModels/HomeViewModel.cs`:

```csharp
namespace ADTool.ViewModels;

public class HomeViewModel : BaseViewModel
{
    public RelayCommand LaunchUPNModifierCommand { get; }
    public RelayCommand LaunchAttributeEditorCommand { get; }

    public HomeViewModel(Action launchUPN, Action launchAttributeEditor)
    {
        LaunchUPNModifierCommand = new RelayCommand(launchUPN);
        LaunchAttributeEditorCommand = new RelayCommand(launchAttributeEditor);
    }
}
```

- [ ] **Step 5: Create UPNToolViewModel.cs**

Create `ADTool/ViewModels/UPNToolViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class UPNToolViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries = new();
    private BaseViewModel _currentStep;
    private readonly BaseViewModel[] _steps;

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public UPNToolViewModel(IAdService adService, CsvImportService csvService, Action returnHome)
    {
        var step1 = new Step1InputViewModel(_entries, csvService, adService, () => GoTo(2));
        var step2 = new Step2ValidateViewModel(_entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new Step3PreviewViewModel(_entries, () => GoTo(2), () => GoTo(4));
        var step4 = new Step4ExecuteViewModel(_entries, adService, Reset);

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        void Reset()
        {
            _entries.Clear();
            returnHome();
        }
    }

    public void GoTo(int stepNumber)
    {
        if (stepNumber < 1 || stepNumber > _steps.Length)
            throw new ArgumentOutOfRangeException(nameof(stepNumber),
                $"Step must be between 1 and {_steps.Length}.");
        CurrentStep = _steps[stepNumber - 1];
    }
}
```

- [ ] **Step 6: Create HomeView.xaml and HomeView.xaml.cs**

Create `ADTool/Views/HomeView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.HomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="#F5F5F5">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <TextBlock Text="AD Tool" FontSize="28" FontWeight="Bold"
                       HorizontalAlignment="Center" Margin="0,0,0,6"/>
            <TextBlock Text="Choose a tool to get started" FontSize="13"
                       Foreground="#666" HorizontalAlignment="Center" Margin="0,0,0,40"/>
            <StackPanel Orientation="Horizontal">
                <!-- UPN Modifier card -->
                <Border Width="260" Margin="0,0,16,0" Background="White"
                        BorderBrush="#DDD" BorderThickness="1" CornerRadius="4" Padding="24">
                    <StackPanel>
                        <TextBlock Text="UPN Bulk Modifier" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,8"/>
                        <TextBlock Text="Change user UPNs and proxy addresses in bulk"
                                   TextWrapping="Wrap" Foreground="#555" Margin="0,0,0,20"/>
                        <Button Content="Launch" Command="{Binding LaunchUPNModifierCommand}"
                                Padding="12,6" HorizontalAlignment="Left"/>
                    </StackPanel>
                </Border>
                <!-- Attribute Editor card -->
                <Border Width="260" Background="White"
                        BorderBrush="#DDD" BorderThickness="1" CornerRadius="4" Padding="24">
                    <StackPanel>
                        <TextBlock Text="Attribute Editor" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,8"/>
                        <TextBlock Text="Bulk-set Department, custom attributes, and other AD fields"
                                   TextWrapping="Wrap" Foreground="#555" Margin="0,0,0,20"/>
                        <Button Content="Launch" Command="{Binding LaunchAttributeEditorCommand}"
                                Padding="12,6" HorizontalAlignment="Left"/>
                    </StackPanel>
                </Border>
            </StackPanel>
        </StackPanel>
    </Grid>
</UserControl>
```

Create `ADTool/Views/HomeView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class HomeView : UserControl
{
    public HomeView() { InitializeComponent(); }
}
```

- [ ] **Step 7: Create UPNToolView.xaml and UPNToolView.xaml.cs**

Create `ADTool/Views/UPNToolView.xaml` (the step indicator and DataTemplates move here from MainWindow):

```xml
<UserControl x:Class="ADTool.Views.UPNToolView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:ADTool.ViewModels"
             xmlns:views="clr-namespace:ADTool.Views">
    <UserControl.Resources>
        <DataTemplate DataType="{x:Type vm:Step1InputViewModel}">
            <views:Step1InputView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:Step2ValidateViewModel}">
            <views:Step2ValidateView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:Step3PreviewViewModel}">
            <views:Step3PreviewView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:Step4ExecuteViewModel}">
            <views:Step4ExecuteView />
        </DataTemplate>
        <Style x:Key="StepLabel" TargetType="TextBlock">
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="4,0"/>
        </Style>
    </UserControl.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="#2D2D30" Padding="16,8">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="1  Input"    Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="2  Validate" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="3  Preview"  Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="4  Execute"  Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
            </StackPanel>
        </Border>
        <ContentControl Grid.Row="1" Content="{Binding CurrentStep}" Margin="16"/>
    </Grid>
</UserControl>
```

Create `ADTool/Views/UPNToolView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class UPNToolView : UserControl
{
    public UPNToolView() { InitializeComponent(); }
}
```

- [ ] **Step 8: Rewrite MainWindow.xaml**

Replace the full content of `ADTool/Views/MainWindow.xaml`:

```xml
<Window x:Class="ADTool.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:ADTool.ViewModels"
        xmlns:views="clr-namespace:ADTool.Views"
        Title="{Binding WindowTitle}"
        Height="640" Width="960"
        MinHeight="480" MinWidth="720"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <DataTemplate DataType="{x:Type vm:HomeViewModel}">
            <views:HomeView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:UPNToolViewModel}">
            <views:UPNToolView />
        </DataTemplate>
        <!-- AttributeToolViewModel DataTemplate added in Task 8 -->
    </Window.Resources>

    <ContentControl Content="{Binding CurrentView}"/>
</Window>
```

- [ ] **Step 9: Update App.xaml.cs**

Replace `ADTool/App.xaml.cs` content:

```csharp
using ADTool.Services;
using ADTool.ViewModels;
using ADTool.Views;
using System.Windows;

namespace ADTool;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool dryRun = e.Args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        IAdService adService = dryRun ? new AdServiceStub() : new AdService();

        if (!dryRun)
        {
            bool isAdmin = await adService.CheckIsDomainAdminAsync();
            if (!isAdmin)
            {
                MessageBox.Show(
                    "This tool requires Domain Admin privileges.\n\nYour account is not a member of the Domain Admins group.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        var shellVm = new AppShellViewModel(adService, new CsvImportService());
        var window = new MainWindow { DataContext = shellVm };
        window.Show();
    }
}
```

- [ ] **Step 10: Delete old files**

Delete `ADTool/ViewModels/MainViewModel.cs`.
Delete `ADTool.Tests/MainViewModelTests.cs`.

- [ ] **Step 11: Run the new tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AppShellViewModelTests|UPNToolViewModelTests"
```

Expected: 10 passing.

- [ ] **Step 12: Build to confirm no XAML errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 13: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass (was 79; now 89 — 10 new, 4 old MainViewModelTests deleted).

- [ ] **Step 14: Commit**

```
git add -A
git commit -m "feat: replace MainViewModel with AppShellViewModel and home screen launcher"
```

---

## Task 2: AttributeChangeEntry model and AttributeColumnMap

**Files:**
- Create: `ADTool/Models/AttributeChangeEntry.cs`
- Create: `ADTool/Models/AttributeColumnMap.cs`
- Create: `ADTool.Tests/AttributeColumnMapTests.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttributeColumnMapTests.cs`:

```csharp
using ADTool.Models;

namespace ADTool.Tests;

public class AttributeColumnMapTests
{
    [Theory]
    [InlineData("UPN")]
    [InlineData("upn")]
    [InlineData("UserPrincipalName")]
    [InlineData("userprincipalname")]
    public void Resolve_IdentityColumn_ReturnsNull(string header)
    {
        Assert.Null(AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("Department",   "department")]
    [InlineData("DEPARTMENT",   "department")]
    [InlineData("Title",        "title")]
    [InlineData("Company",      "company")]
    [InlineData("Office",       "physicalDeliveryOfficeName")]
    [InlineData("Phone",        "telephoneNumber")]
    [InlineData("Manager",      "manager")]
    [InlineData("Description",  "description")]
    public void Resolve_WellKnownHeader_ReturnsLdapName(string header, string expectedLdap)
    {
        Assert.Equal(expectedLdap, AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("CustomAttribute1",  "extensionAttribute1")]
    [InlineData("customattribute1",  "extensionAttribute1")]
    [InlineData("CustomAttribute15", "extensionAttribute15")]
    [InlineData("customattribute7",  "extensionAttribute7")]
    public void Resolve_CustomAttributeHeader_ReturnsExtensionAttribute(string header, string expectedLdap)
    {
        Assert.Equal(expectedLdap, AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("msDS-cloudExtensionAttribute1")]
    [InlineData("employeeID")]
    [InlineData("someRawLdapName")]
    public void Resolve_UnknownHeader_ReturnsHeaderVerbatim(string header)
    {
        Assert.Equal(header, AttributeColumnMap.Resolve(header));
    }

    [Fact]
    public void WellKnownAttributes_HasExpectedCount()
    {
        // 8 HR attributes + 15 custom = 23
        Assert.Equal(23, AttributeColumnMap.WellKnownAttributes.Count);
    }
}
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttributeColumnMapTests"
```

Expected: build error — `AttributeColumnMap` not found.

- [ ] **Step 3: Create AttributeChangeEntry.cs**

Create `ADTool/Models/AttributeChangeEntry.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADTool.Models;

public class AttributeChangeEntry : INotifyPropertyChanged
{
    private string _userUpn = string.Empty;
    private string? _displayName;
    private ValidationStatus _validationStatus;
    private ExecutionStatus _executionStatus;
    private string? _errorTitle;
    private string? _errorDetail;

    public string UserUPN
    {
        get => _userUpn;
        set { _userUpn = value; OnPropertyChanged(); }
    }

    public string? DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public ValidationStatus ValidationStatus
    {
        get => _validationStatus;
        set { _validationStatus = value; OnPropertyChanged(); }
    }

    public ExecutionStatus ExecutionStatus
    {
        get => _executionStatus;
        set { _executionStatus = value; OnPropertyChanged(); }
    }

    public string? ErrorTitle
    {
        get => _errorTitle;
        set { _errorTitle = value; OnPropertyChanged(); }
    }

    public string? ErrorDetail
    {
        get => _errorDetail;
        set { _errorDetail = value; OnPropertyChanged(); }
    }

    // LDAP attribute name → value to write. Empty/null values are skipped at execute time.
    public Dictionary<string, string?> Attributes { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

- [ ] **Step 4: Create AttributeColumnMap.cs**

Create `ADTool/Models/AttributeColumnMap.cs`:

```csharp
namespace ADTool.Models;

/// <summary>
/// Maps CSV column headers to AD LDAP attribute names.
/// Unknown headers pass through verbatim as raw LDAP attribute names (advanced mode).
/// </summary>
public static class AttributeColumnMap
{
    /// <summary>All well-known attributes shown in the Add Column picker.</summary>
    public static readonly IReadOnlyList<(string DisplayName, string LdapName)> WellKnownAttributes =
    [
        ("Department",          "department"),
        ("Description",         "description"),
        ("Title",               "title"),
        ("Company",             "company"),
        ("Office",              "physicalDeliveryOfficeName"),
        ("Phone",               "telephoneNumber"),
        ("Manager",             "manager"),
        ("Employee ID",         "employeeID"),
        ("Custom Attribute 1",  "extensionAttribute1"),
        ("Custom Attribute 2",  "extensionAttribute2"),
        ("Custom Attribute 3",  "extensionAttribute3"),
        ("Custom Attribute 4",  "extensionAttribute4"),
        ("Custom Attribute 5",  "extensionAttribute5"),
        ("Custom Attribute 6",  "extensionAttribute6"),
        ("Custom Attribute 7",  "extensionAttribute7"),
        ("Custom Attribute 8",  "extensionAttribute8"),
        ("Custom Attribute 9",  "extensionAttribute9"),
        ("Custom Attribute 10", "extensionAttribute10"),
        ("Custom Attribute 11", "extensionAttribute11"),
        ("Custom Attribute 12", "extensionAttribute12"),
        ("Custom Attribute 13", "extensionAttribute13"),
        ("Custom Attribute 14", "extensionAttribute14"),
        ("Custom Attribute 15", "extensionAttribute15"),
    ];

    public static readonly IReadOnlySet<string> IdentityHeaders =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UPN", "UserPrincipalName" };

    private static readonly Dictionary<string, string> _aliasMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Department"]        = "department",
            ["Description"]       = "description",
            ["Title"]             = "title",
            ["Company"]           = "company",
            ["Office"]            = "physicalDeliveryOfficeName",
            ["Phone"]             = "telephoneNumber",
            ["Manager"]           = "manager",
            ["EmployeeID"]        = "employeeID",
            ["CustomAttribute1"]  = "extensionAttribute1",
            ["CustomAttribute2"]  = "extensionAttribute2",
            ["CustomAttribute3"]  = "extensionAttribute3",
            ["CustomAttribute4"]  = "extensionAttribute4",
            ["CustomAttribute5"]  = "extensionAttribute5",
            ["CustomAttribute6"]  = "extensionAttribute6",
            ["CustomAttribute7"]  = "extensionAttribute7",
            ["CustomAttribute8"]  = "extensionAttribute8",
            ["CustomAttribute9"]  = "extensionAttribute9",
            ["CustomAttribute10"] = "extensionAttribute10",
            ["CustomAttribute11"] = "extensionAttribute11",
            ["CustomAttribute12"] = "extensionAttribute12",
            ["CustomAttribute13"] = "extensionAttribute13",
            ["CustomAttribute14"] = "extensionAttribute14",
            ["CustomAttribute15"] = "extensionAttribute15",
        };

    /// <summary>
    /// Returns the LDAP attribute name for the given CSV header.
    /// Returns null if the header is an identity column (UPN/UserPrincipalName).
    /// Returns the header verbatim if not in the alias map.
    /// </summary>
    public static string? Resolve(string header)
    {
        if (IdentityHeaders.Contains(header)) return null;
        return _aliasMap.TryGetValue(header, out var ldap) ? ldap : header;
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttributeColumnMapTests"
```

Expected: all passing. (Count test may need adjustment if WellKnownAttributes list differs — update `Assert.Equal(23, ...)` to match actual count.)

- [ ] **Step 6: Run full suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```
git add ADTool/Models/AttributeChangeEntry.cs ADTool/Models/AttributeColumnMap.cs ADTool.Tests/AttributeColumnMapTests.cs
git commit -m "feat: add AttributeChangeEntry model and AttributeColumnMap"
```

---

## Task 3: Extend IAdService — ValidateUserExistsAsync and UpdateAttributesAsync

**Files:**
- Modify: `ADTool/Services/IAdService.cs`
- Modify: `ADTool/Services/AdServiceStub.cs`
- Modify: `ADTool/Services/AdService.cs`

No new unit tests — both new methods require a live AD connection.

---

- [ ] **Step 1: Add methods to IAdService.cs**

In `ADTool/Services/IAdService.cs`, add two methods to the interface (after `GetUsersInOuAsync`):

```csharp
    Task<ValidationResult> ValidateUserExistsAsync(string upn);
    Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes);
```

The full updated interface section:

```csharp
public interface IAdService
{
    Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn);
    Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn);
    Task<bool> CheckIsDomainAdminAsync();
    Task<IReadOnlyList<OuNode>> GetOuTreeAsync();
    Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName);
    Task<ValidationResult> ValidateUserExistsAsync(string upn);
    Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes);
}
```

- [ ] **Step 2: Implement in AdServiceStub.cs**

Add to `ADTool/Services/AdServiceStub.cs` (inside the class, after `GetUsersInOuAsync`):

```csharp
    public Task<ValidationResult> ValidateUserExistsAsync(string upn)
    {
        string displayName = $"[Stub] {(upn.Contains('@') ? upn.Split('@')[0] : upn)}";
        return Task.FromResult(new ValidationResult(true, displayName));
    }

    public Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes)
        => Task.FromResult(new ExecutionResult(true));
```

- [ ] **Step 3: Implement in AdService.cs**

Add to `ADTool/Services/AdService.cs` (inside the class, before the closing `}`):

```csharp
    public async Task<ValidationResult> ValidateUserExistsAsync(string upn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
                if (user is null)
                    return new ValidationResult(false, null, ValidationType.UserNotFound);
                return new ValidationResult(true, user.DisplayName);
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, null, ValidationType.UserNotFound, ex.Message);
            }
        });
    }

    public async Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
                if (user is null)
                    return new ExecutionResult(false, ExecutionErrorType.UnexpectedError,
                        "User not found at execution time.");

                var de = (DirectoryEntry)user.GetUnderlyingObject();
                foreach (var (ldapName, value) in attributes)
                    de.Properties[ldapName].Value = value;
                de.CommitChanges();

                return new ExecutionResult(true);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ExecutionResult(false, ExecutionErrorType.InsufficientPermissions, ex.Message);
            }
            catch (Exception ex)
            {
                return new ExecutionResult(false, ExecutionErrorType.UnexpectedError, ex.Message);
            }
        });
    }
```

- [ ] **Step 4: Build to confirm no errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```
git add ADTool/Services/IAdService.cs ADTool/Services/AdServiceStub.cs ADTool/Services/AdService.cs
git commit -m "feat: add ValidateUserExistsAsync and UpdateAttributesAsync to IAdService"
```

---

## Task 4: AttrStep2ValidateViewModel and AttrStep2ValidateView

**Files:**
- Create: `ADTool.Tests/AttrStep2ValidateViewModelTests.cs`
- Create: `ADTool/ViewModels/AttrStep2ValidateViewModel.cs`
- Create: `ADTool/Views/AttrStep2ValidateView.xaml` + `.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttrStep2ValidateViewModelTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep2ValidateViewModelTests"
```

Expected: build error — `AttrStep2ValidateViewModel` not found.

- [ ] **Step 3: Create AttrStep2ValidateViewModel.cs**

Create `ADTool/ViewModels/AttrStep2ValidateViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class AttrStep2ValidateViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isValidating;
    private int _validatedCount;

    public ObservableCollection<AttributeChangeEntry> Entries => _entries;

    public bool IsValidating
    {
        get => _isValidating;
        private set { SetField(ref _isValidating, value); NextCommand.RaiseCanExecuteChanged(); }
    }

    public int ValidatedCount
    {
        get => _validatedCount;
        private set => SetField(ref _validatedCount, value);
    }

    public int TotalCount => _entries.Count;

    public bool HasInvalidRows =>
        _entries.Any(e => e.ValidationStatus != ValidationStatus.Valid
                       && e.ValidationStatus != ValidationStatus.Pending);

    public RelayCommand BackCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand RemoveInvalidRowsCommand { get; }

    public AttrStep2ValidateViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        IAdService adService,
        Action onBack,
        Action onNext)
    {
        _entries = entries;
        _adService = adService;
        BackCommand = new RelayCommand(onBack);
        NextCommand = new RelayCommand(onNext, CanGoNext);
        RemoveInvalidRowsCommand = new RelayCommand(RemoveInvalidRows);
    }

    public async Task ValidateAllAsync()
    {
        IsValidating = true;
        ValidatedCount = 0;
        OnPropertyChanged(nameof(TotalCount));

        // Pre-pass: flag same-batch duplicate UPNs without hitting AD
        var batchDuplicates = _entries
            .GroupBy(e => e.UserUPN, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToHashSet();

        foreach (var entry in batchDuplicates)
        {
            entry.ValidationStatus = ValidationStatus.DuplicateNewUPN;
            entry.ErrorTitle = "Duplicate user in batch";
            entry.ErrorDetail = $"The UPN '{entry.UserUPN}' appears more than once in this batch. Each user can only appear once per run.";
            Interlocked.Increment(ref _validatedCount);
            OnPropertyChanged(nameof(ValidatedCount));
        }

        var tasks = _entries.Where(e => !batchDuplicates.Contains(e)).Select(async entry =>
        {
            var result = await _adService.ValidateUserExistsAsync(entry.UserUPN);
            entry.DisplayName = result.DisplayName;
            entry.ValidationStatus = result.IsValid ? ValidationStatus.Valid : ValidationStatus.NotFound;

            if (!result.IsValid)
            {
                entry.ErrorTitle = "User not found";
                entry.ErrorDetail = $"No user with UPN '{entry.UserUPN}' exists in Active Directory.";
            }

            Interlocked.Increment(ref _validatedCount);
            OnPropertyChanged(nameof(ValidatedCount));
        });

        await Task.WhenAll(tasks);

        IsValidating = false;
        OnPropertyChanged(nameof(HasInvalidRows));
        NextCommand.RaiseCanExecuteChanged();
        RemoveInvalidRowsCommand.RaiseCanExecuteChanged();
    }

    private bool CanGoNext() =>
        !_isValidating && _entries.Any() && _entries.All(e => e.ValidationStatus == ValidationStatus.Valid);

    private void RemoveInvalidRows()
    {
        var invalid = _entries.Where(e => e.ValidationStatus != ValidationStatus.Valid).ToList();
        foreach (var entry in invalid)
            _entries.Remove(entry);
        OnPropertyChanged(nameof(HasInvalidRows));
        NextCommand.RaiseCanExecuteChanged();
    }
}
```

- [ ] **Step 4: Create AttrStep2ValidateView.xaml and code-behind**

Create `ADTool/Views/AttrStep2ValidateView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.AttrStep2ValidateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:ADTool.Models"
             Loaded="OnLoaded">
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </UserControl.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Progress indicator -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8"
                    Visibility="{Binding IsValidating, Converter={StaticResource BoolToVis}}">
            <ProgressBar IsIndeterminate="False" Width="200" Height="8" VerticalAlignment="Center"
                         Minimum="0" Maximum="{Binding TotalCount, Mode=OneWay}" Value="{Binding ValidatedCount, Mode=OneWay}"/>
            <TextBlock Margin="8,0,0,0" VerticalAlignment="Center">
                <Run Text="Validating "/>
                <Run Text="{Binding ValidatedCount, Mode=OneWay}"/>
                <Run Text=" / "/>
                <Run Text="{Binding TotalCount, Mode=OneWay}"/>
            </TextBlock>
        </StackPanel>

        <!-- Results DataGrid -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False" CanUserAddRows="False"
                  CanUserDeleteRows="False" IsReadOnly="True" HeadersVisibility="Column">
            <DataGrid.RowStyle>
                <Style TargetType="DataGridRow">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                            <Setter Property="Background" Value="#FFF0F0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                            <Setter Property="Background" Value="#FFF0F0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Valid}">
                            <Setter Property="Background" Value="#F0FFF0"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </DataGrid.RowStyle>
            <DataGrid.Columns>
                <DataGridTemplateColumn Header="" Width="30">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock HorizontalAlignment="Center" FontSize="14">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Valid}">
                                                <Setter Property="Text" Value="&#x2714;"/>
                                                <Setter Property="Foreground" Value="Green"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Pending}">
                                                <Setter Property="Text" Value="&#x2026;"/>
                                                <Setter Property="Foreground" Value="Gray"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                                                <Setter Property="Text" Value="&#x2718;"/>
                                                <Setter Property="Foreground" Value="Red"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                                                <Setter Property="Text" Value="&#x2718;"/>
                                                <Setter Property="Foreground" Value="Red"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="UPN"          Binding="{Binding UserUPN}"         Width="*"/>
                <DataGridTextColumn Header="Display Name" Binding="{Binding DisplayName}"     Width="160"/>
                <DataGridTextColumn Header="Status"       Binding="{Binding ValidationStatus}" Width="120"/>
                <DataGridTextColumn Header="Error"        Binding="{Binding ErrorTitle}"      Width="160"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Warning banner -->
        <Border Grid.Row="2" Background="#FFF3CD" BorderBrush="#FFC107" BorderThickness="1"
                CornerRadius="3" Padding="10,6" Margin="0,8,0,0"
                Visibility="{Binding HasInvalidRows, Converter={StaticResource BoolToVis}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="&#x26A0;  Some users were not found or have errors. " VerticalAlignment="Center"/>
                <Button Command="{Binding RemoveInvalidRowsCommand}"
                        Background="Transparent" BorderThickness="0" Foreground="#856404"
                        Cursor="Hand" Padding="0">
                    <TextBlock Text="Remove invalid rows" TextDecorations="Underline"/>
                </Button>
            </StackPanel>
        </Border>

        <!-- Navigation -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="&#x2190; Back" Command="{Binding BackCommand}" Padding="12,5"/>
            <Button Grid.Column="2" Content="Next: Preview &#x2192;" Command="{Binding NextCommand}"
                    Padding="12,5" Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

Create `ADTool/Views/AttrStep2ValidateView.xaml.cs`:

```csharp
using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep2ValidateView : UserControl
{
    public AttrStep2ValidateView() { InitializeComponent(); }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttrStep2ValidateViewModel vm)
            await vm.ValidateAllAsync();
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep2ValidateViewModelTests"
```

Expected: 6 passing.

- [ ] **Step 6: Build to confirm no XAML errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Run full suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 8: Commit**

```
git add ADTool/ViewModels/AttrStep2ValidateViewModel.cs ADTool/Views/AttrStep2ValidateView.xaml ADTool/Views/AttrStep2ValidateView.xaml.cs ADTool.Tests/AttrStep2ValidateViewModelTests.cs
git commit -m "feat: add AttrStep2ValidateViewModel and view"
```

---

## Task 5: AttrStep3PreviewViewModel and AttrStep3PreviewView

**Files:**
- Create: `ADTool.Tests/AttrStep3PreviewViewModelTests.cs`
- Create: `ADTool/ViewModels/AttrStep3PreviewViewModel.cs`
- Create: `ADTool/Views/AttrStep3PreviewView.xaml` + `.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttrStep3PreviewViewModelTests.cs`:

```csharp
using ADTool.Models;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep3PreviewViewModelTests
{
    [Fact]
    public void PreviewTable_HasDisplayNameAndUPNColumns()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", DisplayName = "Alice",
                    Attributes = { ["department"] = "IT" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        Assert.True(vm.PreviewTable.Columns.Contains("Display Name"));
        Assert.True(vm.PreviewTable.Columns.Contains("UPN"));
    }

    [Fact]
    public void PreviewTable_HasColumnForEachAttributeKey()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com",
                    Attributes = { ["department"] = "IT", ["title"] = "Dev" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        Assert.True(vm.PreviewTable.Columns.Contains("department"));
        Assert.True(vm.PreviewTable.Columns.Contains("title"));
    }

    [Fact]
    public void PreviewTable_RowCountMatchesEntryCount()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@contoso.com", Attributes = { ["department"] = "IT" } },
            new() { UserUPN = "b@contoso.com", Attributes = { ["department"] = "HR" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        Assert.Equal(2, vm.PreviewTable.Rows.Count);
    }

    [Fact]
    public void PreviewTable_RowValuesMatchEntryData()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "alice@contoso.com", DisplayName = "Alice Smith",
                    Attributes = { ["department"] = "Engineering" } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        var row = vm.PreviewTable.Rows[0];
        Assert.Equal("alice@contoso.com", row["UPN"]);
        Assert.Equal("Alice Smith",       row["Display Name"]);
        Assert.Equal("Engineering",       row["department"]);
    }

    [Fact]
    public void PreviewTable_MissingAttributeInRow_ShowsEmpty()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>
        {
            new() { UserUPN = "a@c.com", Attributes = { ["department"] = "IT" } },
            new() { UserUPN = "b@c.com", Attributes = { /* no department */ } }
        };
        var vm = new AttrStep3PreviewViewModel(entries, () => { }, () => { });
        Assert.Equal("", vm.PreviewTable.Rows[1]["department"]);
    }
}
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep3PreviewViewModelTests"
```

Expected: build error — `AttrStep3PreviewViewModel` not found.

- [ ] **Step 3: Create AttrStep3PreviewViewModel.cs**

Create `ADTool/ViewModels/AttrStep3PreviewViewModel.cs`:

```csharp
using ADTool.Models;
using System.Collections.ObjectModel;
using System.Data;

namespace ADTool.ViewModels;

public class AttrStep3PreviewViewModel : BaseViewModel
{
    public DataTable PreviewTable { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand NextCommand { get; }

    public int EntryCount { get; }

    public AttrStep3PreviewViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        Action onBack,
        Action onNext)
    {
        BackCommand = new RelayCommand(onBack);
        NextCommand = new RelayCommand(onNext);
        EntryCount = entries.Count;
        PreviewTable = BuildPreviewTable(entries);
    }

    private static DataTable BuildPreviewTable(IEnumerable<AttributeChangeEntry> entries)
    {
        var table = new DataTable();
        var list = entries.ToList();

        table.Columns.Add("Display Name", typeof(string));
        table.Columns.Add("UPN", typeof(string));

        var attrKeys = list
            .SelectMany(e => e.Attributes.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var key in attrKeys)
            table.Columns.Add(key, typeof(string));

        foreach (var entry in list)
        {
            var row = table.NewRow();
            row["Display Name"] = entry.DisplayName ?? "";
            row["UPN"]          = entry.UserUPN;
            foreach (var key in attrKeys)
                row[key] = entry.Attributes.TryGetValue(key, out var val) ? val ?? "" : "";
            table.Rows.Add(row);
        }

        return table;
    }
}
```

- [ ] **Step 4: Create AttrStep3PreviewView.xaml and code-behind**

Create `ADTool/Views/AttrStep3PreviewView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.AttrStep3PreviewView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Info banner -->
        <Border Grid.Row="0" Background="#E8F5E9" BorderBrush="#4CAF50" BorderThickness="1"
                CornerRadius="3" Padding="10,6" Margin="0,0,0,8">
            <TextBlock>
                <Run Text="&#x2714;  "/>
                <Run Text="{Binding EntryCount, Mode=OneWay}"/>
                <Run Text=" users ready. Review the attribute changes below — this cannot be undone."/>
            </TextBlock>
        </Border>

        <!-- Preview DataGrid with auto-generated columns from DataTable -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding PreviewTable}"
                  AutoGenerateColumns="True"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"
                  HeadersVisibility="Column"/>

        <!-- Navigation -->
        <Grid Grid.Row="2" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="&#x2190; Back" Command="{Binding BackCommand}" Padding="12,5"/>
            <Button Grid.Column="2" Content="Execute Changes"
                    Command="{Binding NextCommand}"
                    Padding="16,6" Background="#D32F2F" Foreground="White" FontWeight="Bold"/>
        </Grid>
    </Grid>
</UserControl>
```

Create `ADTool/Views/AttrStep3PreviewView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep3PreviewView : UserControl
{
    public AttrStep3PreviewView() { InitializeComponent(); }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep3PreviewViewModelTests"
```

Expected: 5 passing.

- [ ] **Step 6: Build and run full suite**

```
dotnet build ADTool/ADTool.csproj
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: build succeeded, all tests pass.

- [ ] **Step 7: Commit**

```
git add ADTool/ViewModels/AttrStep3PreviewViewModel.cs ADTool/Views/AttrStep3PreviewView.xaml ADTool/Views/AttrStep3PreviewView.xaml.cs ADTool.Tests/AttrStep3PreviewViewModelTests.cs
git commit -m "feat: add AttrStep3PreviewViewModel and view"
```

---

## Task 6: AttrStep4ExecuteViewModel and AttrStep4ExecuteView

**Files:**
- Create: `ADTool.Tests/AttrStep4ExecuteViewModelTests.cs`
- Create: `ADTool/ViewModels/AttrStep4ExecuteViewModel.cs`
- Create: `ADTool/Views/AttrStep4ExecuteView.xaml` + `.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttrStep4ExecuteViewModelTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep4ExecuteViewModelTests"
```

Expected: build error — `AttrStep4ExecuteViewModel` not found.

- [ ] **Step 3: Create AttrStep4ExecuteViewModel.cs**

Create `ADTool/ViewModels/AttrStep4ExecuteViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace ADTool.ViewModels;

public class AttrStep4ExecuteViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isExecuting;
    private int _successCount;
    private int _failCount;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set => SetField(ref _isExecuting, value);
    }

    public int SuccessCount
    {
        get => _successCount;
        private set => SetField(ref _successCount, value);
    }

    public int FailCount
    {
        get => _failCount;
        private set => SetField(ref _failCount, value);
    }

    public ObservableCollection<AttributeChangeEntry> Entries => _entries;

    public RelayCommand ExportResultsCommand { get; }
    public RelayCommand StartNewRunCommand { get; }

    public AttrStep4ExecuteViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        IAdService adService,
        Action onReset)
    {
        _entries = entries;
        _adService = adService;
        ExportResultsCommand = new RelayCommand(ExportResults);
        StartNewRunCommand = new RelayCommand(onReset);
    }

    public async Task ExecuteAllAsync()
    {
        IsExecuting = true;
        SuccessCount = 0;
        FailCount = 0;

        foreach (var entry in _entries)
        {
            var attrsToWrite = entry.Attributes
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value!);

            var result = await _adService.UpdateAttributesAsync(entry.UserUPN, attrsToWrite);
            entry.ExecutionStatus = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed;

            if (result.Success)
            {
                SuccessCount++;
            }
            else
            {
                (entry.ErrorTitle, entry.ErrorDetail) =
                    ErrorMessages.ForExecutionFailure(result.ErrorType, result.TechnicalDetail);
                FailCount++;
            }
        }

        IsExecuting = false;
    }

    private void ExportResults()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"attr-results-{DateTime.Now:yyyy-MM-dd-HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        using var writer = new StreamWriter(dlg.FileName);
        writer.WriteLine("UPN,DisplayName,Status,ErrorTitle,ErrorDetail");
        foreach (var e in _entries)
            writer.WriteLine($"{Escape(e.UserUPN)},{Escape(e.DisplayName ?? "")}," +
                             $"{e.ExecutionStatus},{Escape(e.ErrorTitle ?? "")},{Escape(e.ErrorDetail ?? "")}");
    }

    private static string Escape(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
```

- [ ] **Step 4: Create AttrStep4ExecuteView.xaml and code-behind**

Create `ADTool/Views/AttrStep4ExecuteView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.AttrStep4ExecuteView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:ADTool.Models"
             Loaded="OnLoaded">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Results list -->
        <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding Entries}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Margin="0,0,0,4" Padding="10,8" CornerRadius="3" BorderThickness="1">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="Background" Value="#F0FFF0"/>
                                    <Setter Property="BorderBrush" Value="#81C784"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                            <Setter Property="Background" Value="#FFF0F0"/>
                                            <Setter Property="BorderBrush" Value="#E57373"/>
                                        </DataTrigger>
                                        <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                            <Setter Property="Background" Value="#FAFAFA"/>
                                            <Setter Property="BorderBrush" Value="#BDBDBD"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </Border.Style>
                            <StackPanel>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="24"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <TextBlock Grid.Column="0" FontSize="14" VerticalAlignment="Center">
                                        <TextBlock.Style>
                                            <Style TargetType="TextBlock">
                                                <Style.Triggers>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Success}">
                                                        <Setter Property="Text" Value="&#x2714;"/>
                                                        <Setter Property="Foreground" Value="Green"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                                        <Setter Property="Text" Value="&#x2718;"/>
                                                        <Setter Property="Foreground" Value="Red"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                                        <Setter Property="Text" Value="&#x2026;"/>
                                                        <Setter Property="Foreground" Value="Gray"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </TextBlock.Style>
                                    </TextBlock>
                                    <StackPanel Grid.Column="1">
                                        <TextBlock FontWeight="SemiBold" Text="{Binding DisplayName}"/>
                                        <TextBlock Foreground="Gray" FontSize="11" Text="{Binding UserUPN}"/>
                                    </StackPanel>
                                    <TextBlock Grid.Column="2" VerticalAlignment="Center" FontSize="11"
                                               Text="{Binding ExecutionStatus}" Foreground="Gray" Margin="8,0,0,0"/>
                                </Grid>
                                <Expander Margin="24,6,0,0" Header="{Binding ErrorTitle}" Foreground="Red">
                                    <Expander.Style>
                                        <Style TargetType="Expander">
                                            <Setter Property="Visibility" Value="Collapsed"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                                    <Setter Property="Visibility" Value="Visible"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Expander.Style>
                                    <Border Background="#FFF3F3" Padding="8,6" Margin="0,4,0,0" CornerRadius="2">
                                        <TextBlock Text="{Binding ErrorDetail}" TextWrapping="Wrap"/>
                                    </Border>
                                </Expander>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Footer -->
        <Grid Grid.Row="1" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center" Foreground="Gray" FontSize="11">
                <Run Text="{Binding SuccessCount, Mode=OneWay}"/>
                <Run Text=" succeeded  &#xB7;  "/>
                <Run Text="{Binding FailCount, Mode=OneWay}"/>
                <Run Text=" failed"/>
            </TextBlock>
            <Button Grid.Column="1" Content="Export Results CSV"
                    Command="{Binding ExportResultsCommand}"
                    Padding="10,5" Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Start New Run"
                    Command="{Binding StartNewRunCommand}"
                    Padding="10,5" Background="#1976D2" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

Create `ADTool/Views/AttrStep4ExecuteView.xaml.cs`:

```csharp
using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep4ExecuteView : UserControl
{
    public AttrStep4ExecuteView() { InitializeComponent(); }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttrStep4ExecuteViewModel vm)
            await vm.ExecuteAllAsync();
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep4ExecuteViewModelTests"
```

Expected: 4 passing.

- [ ] **Step 6: Build and run full suite**

```
dotnet build ADTool/ADTool.csproj
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: build succeeded, all tests pass.

- [ ] **Step 7: Commit**

```
git add ADTool/ViewModels/AttrStep4ExecuteViewModel.cs ADTool/Views/AttrStep4ExecuteView.xaml ADTool/Views/AttrStep4ExecuteView.xaml.cs ADTool.Tests/AttrStep4ExecuteViewModelTests.cs
git commit -m "feat: add AttrStep4ExecuteViewModel and view"
```

---

## Task 7: AttrStep1InputViewModel, AddColumnDialog, and AttrStep1InputView

**Files:**
- Create: `ADTool.Tests/AttrStep1InputViewModelTests.cs`
- Create: `ADTool/ViewModels/AttrStep1InputViewModel.cs`
- Create: `ADTool/Views/AddColumnDialog.xaml` + `.cs`
- Create: `ADTool/Views/AttrStep1InputView.xaml` + `.cs`

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttrStep1InputViewModelTests.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class AttrStep1InputViewModelTests
{
    [Fact]
    public void InputTable_HasUPNColumnOnCreation()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        Assert.True(vm.InputTable.Columns.Contains("UPN"));
    }

    [Fact]
    public void NextCommand_DisabledWhenNoRows()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void AddRow_AddsRowToInputTable()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        vm.AddRowCommand.Execute(null);
        Assert.Equal(1, vm.InputTable.Rows.Count);
    }

    [Fact]
    public void AddUsersFromBrowser_AddsRowsWithUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var users = new List<AdUser> { new("alice@contoso.com", "Alice Smith") };

        vm.AddUsersFromBrowser(users);

        Assert.Equal(1, vm.InputTable.Rows.Count);
        Assert.Equal("alice@contoso.com", vm.InputTable.Rows[0]["UPN"]);
    }

    [Fact]
    public void AddUsersFromBrowser_SkipsDuplicateUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var users = new List<AdUser>
        {
            new("alice@contoso.com", "Alice"),
            new("alice@contoso.com", "Alice Duplicate")
        };

        vm.AddUsersFromBrowser(users);

        Assert.Equal(1, vm.InputTable.Rows.Count);
    }

    [Fact]
    public void Next_PopulatesEntriesFromInputTable()
    {
        bool nextCalled = false;
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => nextCalled = true);

        // Add a column and populate a row directly
        vm.InputTable.Columns.Add("department", typeof(string));
        var row = vm.InputTable.NewRow();
        row["UPN"] = "alice@contoso.com";
        row["department"] = "Engineering";
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.True(nextCalled);
        Assert.Single(entries);
        Assert.Equal("alice@contoso.com", entries[0].UserUPN);
        Assert.Equal("Engineering", entries[0].Attributes["department"]);
    }

    [Fact]
    public void Next_SkipsRowsWithBlankUPN()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        var row = vm.InputTable.NewRow();
        row["UPN"] = "";
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.Empty(entries);
    }

    [Fact]
    public void Next_SkipsBlankAttributeValues()
    {
        var entries = new ObservableCollection<AttributeChangeEntry>();
        var vm = new AttrStep1InputViewModel(entries, new AdServiceStub(), () => { });
        vm.InputTable.Columns.Add("department", typeof(string));
        vm.InputTable.Columns.Add("title", typeof(string));
        var row = vm.InputTable.NewRow();
        row["UPN"] = "alice@contoso.com";
        row["department"] = "IT";
        row["title"] = ""; // blank — should be omitted
        vm.InputTable.Rows.Add(row);

        vm.NextCommand.Execute(null);

        Assert.True(entries[0].Attributes.ContainsKey("department"));
        Assert.False(entries[0].Attributes.ContainsKey("title"));
    }
}
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep1InputViewModelTests"
```

Expected: build error — `AttrStep1InputViewModel` not found.

- [ ] **Step 3: Create AttrStep1InputViewModel.cs**

Create `ADTool/ViewModels/AttrStep1InputViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.Views;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;

namespace ADTool.ViewModels;

public class AttrStep1InputViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private readonly IAdService _adService;
    private readonly Action _onNext;
    private readonly DataTable _inputTable;

    public DataTable InputTable => _inputTable;

    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand AddColumnCommand { get; }
    public RelayCommand OpenAdBrowserCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand NextCommand { get; }

    public AttrStep1InputViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        IAdService adService,
        Action onNext)
    {
        _entries = entries;
        _adService = adService;
        _onNext = onNext;

        _inputTable = new DataTable();
        _inputTable.Columns.Add("UPN", typeof(string));

        ImportCsvCommand    = new RelayCommand(ImportCsv);
        AddColumnCommand    = new RelayCommand(AddColumn);
        OpenAdBrowserCommand = new RelayCommand(OpenAdBrowser);
        AddRowCommand       = new RelayCommand(AddRow);
        NextCommand         = new RelayCommand(Next, () => _inputTable.Rows.Count > 0);
    }

    internal void AddUsersFromBrowser(IReadOnlyList<AdUser> users)
    {
        var existing = new HashSet<string>(
            _inputTable.AsEnumerable().Select(r => r["UPN"]?.ToString() ?? ""),
            StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
        {
            if (existing.Contains(user.UPN)) continue;
            var row = _inputTable.NewRow();
            row["UPN"] = user.UPN;
            _inputTable.Rows.Add(row);
            existing.Add(user.UPN);
        }

        NextCommand.RaiseCanExecuteChanged();
    }

    private void ImportCsv()
    {
        var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var reader = new StreamReader(dlg.FileName, System.Text.Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated  = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];

            // Find identity column index; build LDAP mapping for other columns
            int identityIdx = -1;
            var attrCols = new List<(int HeaderIndex, string LdapName)>();

            for (int i = 0; i < headers.Length; i++)
            {
                if (AttributeColumnMap.IdentityHeaders.Contains(headers[i]))
                {
                    identityIdx = i;
                }
                else
                {
                    var ldap = AttributeColumnMap.Resolve(headers[i]);
                    if (ldap != null && !_inputTable.Columns.Contains(ldap))
                    {
                        _inputTable.Columns.Add(ldap, typeof(string));
                        attrCols.Add((i, ldap));
                    }
                }
            }

            if (identityIdx < 0)
            {
                MessageBox.Show("CSV must have a 'UPN' or 'UserPrincipalName' column.",
                    "Import error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            while (csv.Read())
            {
                var upn = csv.GetField(identityIdx)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(upn)) continue;

                var row = _inputTable.NewRow();
                row["UPN"] = upn;
                foreach (var (idx, ldap) in attrCols)
                    row[ldap] = csv.GetField(idx)?.Trim() ?? "";
                _inputTable.Rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import CSV: {ex.Message}", "Import error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        NextCommand.RaiseCanExecuteChanged();
    }

    private void AddColumn()
    {
        var dialog = new AddColumnDialog { Owner = Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true) return;

        foreach (var ldapName in dialog.SelectedLdapNames)
            if (!string.IsNullOrWhiteSpace(ldapName) && !_inputTable.Columns.Contains(ldapName))
                _inputTable.Columns.Add(ldapName, typeof(string));
    }

    private void OpenAdBrowser()
    {
        var vm = new AdBrowserViewModel(_adService, AddUsersFromBrowser);
        var dialog = new AdBrowserDialog(vm) { Owner = Application.Current?.MainWindow };
        dialog.ShowDialog();
    }

    private void AddRow()
    {
        _inputTable.Rows.Add(_inputTable.NewRow());
        NextCommand.RaiseCanExecuteChanged();
    }

    private void Next()
    {
        _entries.Clear();

        foreach (DataRow row in _inputTable.Rows)
        {
            var upn = row["UPN"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(upn)) continue;

            var attrs = new Dictionary<string, string?>();
            foreach (DataColumn col in _inputTable.Columns)
            {
                if (col.ColumnName == "UPN") continue;
                var val = row[col]?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    attrs[col.ColumnName] = val;
            }

            _entries.Add(new AttributeChangeEntry
            {
                UserUPN    = upn,
                Attributes = attrs
            });
        }

        _onNext();
    }
}
```

- [ ] **Step 4: Create AddColumnDialog.xaml and code-behind**

Create `ADTool/Views/AddColumnDialog.xaml`:

```xml
<Window x:Class="ADTool.Views.AddColumnDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Add Columns" Width="380" Height="480"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="Select attributes to add as columns:"
                   FontWeight="SemiBold" Margin="0,0,0,8"/>

        <ListBox Grid.Row="1" x:Name="AttributeList" Margin="0,0,0,12">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <CheckBox IsChecked="{Binding IsChecked}" Content="{Binding DisplayName}" Padding="4,2"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,16">
            <TextBlock Text="Custom LDAP name:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox x:Name="CustomLdapTextBox" Width="190" ToolTip="e.g. msDS-cloudExtensionAttribute1"/>
        </StackPanel>

        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="OK" Click="OkButton_Click" Width="80" Margin="0,0,8,0" IsDefault="True"/>
            <Button Content="Cancel" IsCancel="True" Width="80"/>
        </StackPanel>
    </Grid>
</Window>
```

Create `ADTool/Views/AddColumnDialog.xaml.cs`:

```csharp
using ADTool.Models;
using System.Windows;

namespace ADTool.Views;

public partial class AddColumnDialog : Window
{
    public IReadOnlyList<string> SelectedLdapNames { get; private set; } = [];

    public AddColumnDialog()
    {
        InitializeComponent();
        AttributeList.ItemsSource = AttributeColumnMap.WellKnownAttributes
            .Select(a => new AttributeCheckItem { DisplayName = a.DisplayName, LdapName = a.LdapName })
            .ToList();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ((IEnumerable<AttributeCheckItem>)AttributeList.ItemsSource)
            .Where(i => i.IsChecked)
            .Select(i => i.LdapName)
            .ToList();

        var custom = CustomLdapTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(custom))
            selected.Add(custom);

        SelectedLdapNames = selected;
        DialogResult = true;
    }
}

public class AttributeCheckItem
{
    public string DisplayName { get; set; } = "";
    public string LdapName    { get; set; } = "";
    public bool   IsChecked   { get; set; }
}
```

- [ ] **Step 5: Create AttrStep1InputView.xaml and code-behind**

Create `ADTool/Views/AttrStep1InputView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.AttrStep1InputView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <WrapPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8">
            <Button Content="&#x1F4C2;  Import CSV"
                    Command="{Binding ImportCsvCommand}" Padding="10,5" Margin="0,0,8,0"/>
            <Button Content="&#x1F50D;  Browse AD&#x2026;"
                    Command="{Binding OpenAdBrowserCommand}" Padding="10,5" Margin="0,0,8,0"/>
            <Button Content="+ Add Column"
                    Command="{Binding AddColumnCommand}" Padding="10,5"/>
        </WrapPanel>

        <!-- Hint -->
        <TextBlock Grid.Row="1"
                   Text="Import a CSV (UPN column required) or browse AD to add users. Use 'Add Column' to choose which attributes to set."
                   Foreground="Gray" FontSize="11" Margin="0,0,0,4" TextWrapping="Wrap"/>

        <!-- DataGrid bound to DataTable — columns are dynamic -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding InputTable}"
                  AutoGenerateColumns="True"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  HeadersVisibility="Column"/>

        <!-- Footer -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center" Foreground="Gray" FontSize="11">
                <Run Text="{Binding InputTable.Rows.Count, Mode=OneWay}"/>
                <Run Text=" rows"/>
            </TextBlock>
            <Button Grid.Column="1" Content="+ Add Row"
                    Command="{Binding AddRowCommand}" Padding="8,5" Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Next: Validate &#x2192;"
                    Command="{Binding NextCommand}"
                    Padding="12,5" Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

Create `ADTool/Views/AttrStep1InputView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep1InputView : UserControl
{
    public AttrStep1InputView() { InitializeComponent(); }
}
```

- [ ] **Step 6: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttrStep1InputViewModelTests"
```

Expected: 8 passing.

- [ ] **Step 7: Build to confirm no XAML errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Run full suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 9: Commit**

```
git add ADTool/ViewModels/AttrStep1InputViewModel.cs ADTool/Views/AttrStep1InputView.xaml ADTool/Views/AttrStep1InputView.xaml.cs ADTool/Views/AddColumnDialog.xaml ADTool/Views/AddColumnDialog.xaml.cs ADTool.Tests/AttrStep1InputViewModelTests.cs
git commit -m "feat: add AttrStep1InputViewModel, AddColumnDialog, and AttrStep1InputView"
```

---

## Task 8: AttributeToolViewModel, AttrToolView, and wire into AppShellViewModel

**Files:**
- Create: `ADTool.Tests/AttributeToolViewModelTests.cs`
- Create: `ADTool/ViewModels/AttributeToolViewModel.cs`
- Create: `ADTool/Views/AttrToolView.xaml` + `.cs`
- Modify: `ADTool/ViewModels/AppShellViewModel.cs` (wire `LaunchAttributeEditor`)
- Modify: `ADTool/Views/MainWindow.xaml` (add `AttributeToolViewModel` DataTemplate)

---

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/AttributeToolViewModelTests.cs`:

```csharp
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
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GoTo(5));
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
```

Also add these two tests to `ADTool.Tests/AppShellViewModelTests.cs` (append inside the class):

```csharp
    [Fact]
    public void LaunchAttributeEditor_SetsCurrentViewToAttributeToolViewModel()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchAttributeEditorCommand.Execute(null);
        Assert.IsType<AttributeToolViewModel>(vm.CurrentView);
    }

    [Fact]
    public void WindowTitle_IsAttributeEditor_WhenAttributeToolActive()
    {
        var vm = new AppShellViewModel(_adMock.Object, _csvSvc);
        vm.LaunchAttributeEditorCommand.Execute(null);
        Assert.Equal("AD Tool — Attribute Editor", vm.WindowTitle);
    }
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttributeToolViewModelTests|LaunchAttributeEditor|IsAttributeEditor"
```

Expected: build error — `AttributeToolViewModel` not found.

- [ ] **Step 3: Create AttributeToolViewModel.cs**

Create `ADTool/ViewModels/AttributeToolViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class AttributeToolViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries = new();
    private BaseViewModel _currentStep;
    private readonly BaseViewModel[] _steps;

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public AttributeToolViewModel(IAdService adService, Action returnHome)
    {
        var step1 = new AttrStep1InputViewModel(_entries, adService, () => GoTo(2));
        var step2 = new AttrStep2ValidateViewModel(_entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new AttrStep3PreviewViewModel(_entries, () => GoTo(2), () => GoTo(4));
        var step4 = new AttrStep4ExecuteViewModel(_entries, adService, Reset);

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        void Reset()
        {
            _entries.Clear();
            returnHome();
        }
    }

    public void GoTo(int stepNumber)
    {
        if (stepNumber < 1 || stepNumber > _steps.Length)
            throw new ArgumentOutOfRangeException(nameof(stepNumber),
                $"Step must be between 1 and {_steps.Length}.");
        CurrentStep = _steps[stepNumber - 1];
    }
}
```

- [ ] **Step 4: Create AttrToolView.xaml and code-behind**

Create `ADTool/Views/AttrToolView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.AttrToolView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:ADTool.ViewModels"
             xmlns:views="clr-namespace:ADTool.Views">
    <UserControl.Resources>
        <DataTemplate DataType="{x:Type vm:AttrStep1InputViewModel}">
            <views:AttrStep1InputView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:AttrStep2ValidateViewModel}">
            <views:AttrStep2ValidateView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:AttrStep3PreviewViewModel}">
            <views:AttrStep3PreviewView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:AttrStep4ExecuteViewModel}">
            <views:AttrStep4ExecuteView />
        </DataTemplate>
        <Style x:Key="StepLabel" TargetType="TextBlock">
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="4,0"/>
        </Style>
    </UserControl.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="#2D2D30" Padding="16,8">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="1  Input"    Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="2  Validate" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="3  Preview"  Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › "          Foreground="#555"    Style="{StaticResource StepLabel}"/>
                <TextBlock Text="4  Execute"  Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
            </StackPanel>
        </Border>
        <ContentControl Grid.Row="1" Content="{Binding CurrentStep}" Margin="16"/>
    </Grid>
</UserControl>
```

Create `ADTool/Views/AttrToolView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrToolView : UserControl
{
    public AttrToolView() { InitializeComponent(); }
}
```

- [ ] **Step 5: Wire LaunchAttributeEditor in AppShellViewModel.cs**

In `ADTool/ViewModels/AppShellViewModel.cs`, replace the empty `LaunchAttributeEditor` method:

```csharp
    private void LaunchAttributeEditor()
    {
        CurrentView = new AttributeToolViewModel(_adService, ReturnHome);
    }
```

- [ ] **Step 6: Add AttributeToolViewModel DataTemplate to MainWindow.xaml**

In `ADTool/Views/MainWindow.xaml`, replace the comment line with:

```xml
        <DataTemplate DataType="{x:Type vm:AttributeToolViewModel}">
            <views:AttrToolView />
        </DataTemplate>
```

- [ ] **Step 7: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "AttributeToolViewModelTests|LaunchAttributeEditor|IsAttributeEditor"
```

Expected: 6 passing.

- [ ] **Step 8: Build to confirm no XAML errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 9: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 10: Commit**

```
git add ADTool/ViewModels/AttributeToolViewModel.cs ADTool/Views/AttrToolView.xaml ADTool/Views/AttrToolView.xaml.cs ADTool/ViewModels/AppShellViewModel.cs ADTool/Views/MainWindow.xaml ADTool.Tests/AttributeToolViewModelTests.cs ADTool.Tests/AppShellViewModelTests.cs
git commit -m "feat: add AttributeToolViewModel, AttrToolView, and wire into AppShellViewModel"
```

---

## Task 9: README update and push

**Files:**
- Modify: `README.md`

---

- [ ] **Step 1: Update README.md**

Add a new top-level section after the existing content, before **Building from source**. The full section to insert:

```markdown
---

### Attribute Editor

A separate tool accessible from the home screen for bulk-setting AD attributes across many users in a single run.

#### Supported attributes

| CSV column header | AD attribute |
|---|---|
| `UPN` or `UserPrincipalName` | Identity (not written) |
| `Department` | `department` |
| `Description` | `description` |
| `Title` | `title` |
| `Company` | `company` |
| `Office` | `physicalDeliveryOfficeName` |
| `Phone` | `telephoneNumber` |
| `Manager` | `manager` |
| `EmployeeID` | `employeeID` |
| `CustomAttribute1`–`CustomAttribute15` | `extensionAttribute1`–`extensionAttribute15` |
| Any other column header | Used verbatim as the LDAP attribute name |

Blank cells in a row are skipped — only non-blank values are written to AD.

#### Workflow

**Step 1 — Input.** Populate users and attribute values using any combination of:
- **Import CSV** — a CSV with a `UPN` column and any attribute columns from the table above
- **Browse AD…** — select users from the AD OU tree (same dialog as the UPN Modifier)
- **Add Column** — pick from the well-known attribute list or enter a raw LDAP name
- **Add Row** — type a UPN directly into the grid

**Step 2 — Validate.** Confirms each user exists in AD. Duplicate UPNs within the same batch are flagged without querying AD.

**Step 3 — Preview.** Shows a grid with one column per attribute to be written. Review before committing.

**Step 4 — Execute.** Writes all non-blank attribute values via `DirectoryEntry.CommitChanges()`. Export CSV columns: `UPN`, `DisplayName`, `Status`, `ErrorTitle`, `ErrorDetail`.
```

Also update the **Running the tool** section — add a note that the tool now opens to a home screen:

Find the existing paragraph:
```
The tool checks at startup whether the current user is a member of the **Domain Admins** group. If not, access is denied and the tool exits.
```

Change it to:
```
The tool opens to a home screen where you choose between the **UPN Bulk Modifier** and the **Attribute Editor**. It checks at startup whether the current user is a member of the **Domain Admins** group. If not, access is denied and the tool exits.
```

- [ ] **Step 2: Commit**

```
git add README.md
git commit -m "docs: add Attribute Editor section to README and update home screen note"
```

- [ ] **Step 3: Push**

```
git push
```

---
