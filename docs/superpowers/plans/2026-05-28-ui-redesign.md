# UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle the AD Tool WPF app to a Dark Slate theme with Electric Blue accent, progress-pill step indicators, and a shared ResourceDictionary so all colour and style decisions live in one place.

**Architecture:** A single `ADTool/Themes/Theme.xaml` ResourceDictionary is merged into `App.xaml`. It defines all `SolidColorBrush` tokens, implicit `Button`/`DataGrid`/`TextBox`/etc. styles, and named `PrimaryButtonStyle` / `DangerButtonStyle` variants. A new `StepIndicatorControl` UserControl replaces the hard-coded step bar in both tool shells. Two small ViewModel additions (`CurrentStepNumber`, `ReturnHomeCommand`) are the only logic changes.

**Tech Stack:** WPF .NET 8, XAML ResourceDictionary, xUnit (existing test project at `ADTool.Tests/`)

---

## File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `ADTool/Themes/Theme.xaml` | All brushes + implicit component styles |
| Create | `ADTool/Views/StepIndicatorControl.xaml` | Shared progress-pill UserControl (XAML) |
| Create | `ADTool/Views/StepIndicatorControl.xaml.cs` | DependencyProperties + rebuild logic |
| Modify | `ADTool/App.xaml` | Merge Theme.xaml |
| Modify | `ADTool/Views/MainWindow.xaml` | Set dark background |
| Modify | `ADTool/ViewModels/UPNToolViewModel.cs` | Add CurrentStepNumber + ReturnHomeCommand |
| Modify | `ADTool/ViewModels/AttributeToolViewModel.cs` | Add CurrentStepNumber + ReturnHomeCommand |
| Modify | `ADTool/Views/HomeView.xaml` | Dark cards with accent top border |
| Modify | `ADTool/Views/UPNToolView.xaml` | Title bar + StepIndicatorControl |
| Modify | `ADTool/Views/AttrToolView.xaml` | Title bar + StepIndicatorControl |
| Modify | `ADTool/Views/Step1InputView.xaml` | Dark restyle |
| Modify | `ADTool/Views/Step2ValidateView.xaml` | Dark restyle |
| Modify | `ADTool/Views/Step3PreviewView.xaml` | Dark restyle |
| Modify | `ADTool/Views/Step4ExecuteView.xaml` | Dark restyle |
| Modify | `ADTool/Views/AttrStep1InputView.xaml` | Dark restyle |
| Modify | `ADTool/Views/AttrStep2ValidateView.xaml` | Dark restyle |
| Modify | `ADTool/Views/AttrStep3PreviewView.xaml` | Dark restyle |
| Modify | `ADTool/Views/AttrStep4ExecuteView.xaml` | Dark restyle |
| Modify | `ADTool/Views/AdBrowserDialog.xaml` | Dark restyle |
| Modify | `ADTool/Views/AddColumnDialog.xaml` | Dark restyle |
| Modify | `ADTool.Tests/` | Unit tests for ViewModel additions + StepIndicatorControl logic |

---

## Colour Token Reference

Used throughout all tasks — commit this table to memory:

| Token (Brush resource key) | Hex | Usage |
|---|---|---|
| `BackgroundDeepBrush` | `#1E1E1E` | Window / page background |
| `BackgroundPanelBrush` | `#252526` | Content panels, DataGrid body |
| `BackgroundChromeBrush` | `#2D2D30` | Title bar, step bar, toolbar |
| `BackgroundAltBrush` | `#222222` | Alternating DataGrid rows |
| `BorderSubtleBrush` | `#3C3C3C` | Panel borders, DataGrid row lines |
| `BorderStrongBrush` | `#555555` | Default button borders |
| `AccentBrush` | `#4FC3F7` | Active step, primary button border/text |
| `AccentBgBrush` | `#1A3350` | Primary button bg, selected row tint |
| `AccentBgHoverBrush` | `#1E3D60` | Primary button hover |
| `SuccessBrush` | `#4EC994` | ✓ icon, completed step border |
| `SuccessBgBrush` | `#1C3A2A` | Completed step circle bg |
| `ErrorBrush` | `#F48771` | ✗ icon, error row |
| `ErrorBgBrush` | `#2A1A1A` | Error row bg, danger button bg |
| `ErrorBorderBrush` | `#6A3333` | Danger button border, error row border |
| `WarningBrush` | `#E9C46A` | Warning banner text |
| `TextPrimaryBrush` | `#CCCCCC` | Body text |
| `TextMutedBrush` | `#777777` | Hints, descriptions |
| `TextDimBrush` | `#555555` | Inactive step labels, separators |
| `TextHeaderBrush` | `#999999` | DataGrid column headers |

---

### Task 1: Theme.xaml — Brushes and Button styles

**Files:**
- Create: `ADTool/Themes/Theme.xaml`

- [ ] **Step 1: Create the Themes directory and Theme.xaml with all brush resources and three button styles**

```xml
<!-- ADTool/Themes/Theme.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ═══ Colour tokens ═══ -->
    <SolidColorBrush x:Key="BackgroundDeepBrush"   Color="#1E1E1E"/>
    <SolidColorBrush x:Key="BackgroundPanelBrush"  Color="#252526"/>
    <SolidColorBrush x:Key="BackgroundChromeBrush" Color="#2D2D30"/>
    <SolidColorBrush x:Key="BackgroundAltBrush"    Color="#222222"/>
    <SolidColorBrush x:Key="BorderSubtleBrush"     Color="#3C3C3C"/>
    <SolidColorBrush x:Key="BorderStrongBrush"     Color="#555555"/>
    <SolidColorBrush x:Key="AccentBrush"           Color="#4FC3F7"/>
    <SolidColorBrush x:Key="AccentBgBrush"         Color="#1A3350"/>
    <SolidColorBrush x:Key="AccentBgHoverBrush"    Color="#1E3D60"/>
    <SolidColorBrush x:Key="SuccessBrush"          Color="#4EC994"/>
    <SolidColorBrush x:Key="SuccessBgBrush"        Color="#1C3A2A"/>
    <SolidColorBrush x:Key="ErrorBrush"            Color="#F48771"/>
    <SolidColorBrush x:Key="ErrorBgBrush"          Color="#2A1A1A"/>
    <SolidColorBrush x:Key="ErrorBorderBrush"      Color="#6A3333"/>
    <SolidColorBrush x:Key="WarningBrush"          Color="#E9C46A"/>
    <SolidColorBrush x:Key="TextPrimaryBrush"      Color="#CCCCCC"/>
    <SolidColorBrush x:Key="TextMutedBrush"        Color="#777777"/>
    <SolidColorBrush x:Key="TextDimBrush"          Color="#555555"/>
    <SolidColorBrush x:Key="TextHeaderBrush"       Color="#999999"/>

    <!-- ═══ Shared converter ═══ -->
    <BooleanToVisibilityConverter x:Key="BoolToVis"/>

    <!-- ═══ Default Button (implicit — applies to all Button controls) ═══ -->
    <Style TargetType="Button">
        <Setter Property="Background"   Value="{StaticResource BackgroundChromeBrush}"/>
        <Setter Property="Foreground"   Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush"  Value="{StaticResource BorderStrongBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding"      Value="14,6"/>
        <Setter Property="FontSize"     Value="11"/>
        <Setter Property="FontWeight"   Value="SemiBold"/>
        <Setter Property="FontFamily"   Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="Cursor"       Value="Hand"/>
        <Setter Property="SnapsToDevicePixels" Value="True"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="bd"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="5"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="#363636"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="#1A1A1A"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ═══ Primary Button (AccentBg bg, Accent border/text) ═══ -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background"   Value="{StaticResource AccentBgBrush}"/>
        <Setter Property="Foreground"   Value="{StaticResource AccentBrush}"/>
        <Setter Property="BorderBrush"  Value="{StaticResource AccentBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding"      Value="14,6"/>
        <Setter Property="FontSize"     Value="11"/>
        <Setter Property="FontWeight"   Value="SemiBold"/>
        <Setter Property="FontFamily"   Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="Cursor"       Value="Hand"/>
        <Setter Property="SnapsToDevicePixels" Value="True"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="bd"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="5"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="{StaticResource AccentBgHoverBrush}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="#0D2640"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ═══ Danger Button (ErrorBg bg, ErrorBorder border, Error text) ═══ -->
    <Style x:Key="DangerButtonStyle" TargetType="Button">
        <Setter Property="Background"   Value="{StaticResource ErrorBgBrush}"/>
        <Setter Property="Foreground"   Value="{StaticResource ErrorBrush}"/>
        <Setter Property="BorderBrush"  Value="{StaticResource ErrorBorderBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding"      Value="14,6"/>
        <Setter Property="FontSize"     Value="11"/>
        <Setter Property="FontWeight"   Value="SemiBold"/>
        <Setter Property="FontFamily"   Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="Cursor"       Value="Hand"/>
        <Setter Property="SnapsToDevicePixels" Value="True"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="bd"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="5"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="bd" Property="Background" Value="#3A2020"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Opacity" Value="0.4"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

- [ ] **Step 2: Build to confirm Theme.xaml is valid XML (no code references it yet)**

```
dotnet build ADTool/ADTool.csproj
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add ADTool/Themes/Theme.xaml
git commit -m "feat: add Theme.xaml with dark palette and button styles"
```

---

### Task 2: Theme.xaml — DataGrid and remaining control styles

**Files:**
- Modify: `ADTool/Themes/Theme.xaml`

- [ ] **Step 1: Append DataGrid styles inside the ResourceDictionary (before the closing tag)**

```xml
    <!-- ═══ TextBox ═══ -->
    <Style TargetType="TextBox">
        <Setter Property="Background"      Value="{StaticResource BackgroundChromeBrush}"/>
        <Setter Property="Foreground"      Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush"     Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Padding"         Value="5,4"/>
        <Setter Property="FontFamily"      Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="FontSize"        Value="12"/>
        <Setter Property="CaretBrush"      Value="{StaticResource TextPrimaryBrush}"/>
        <Style.Triggers>
            <Trigger Property="IsFocused" Value="True">
                <Setter Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ═══ DataGrid ═══ -->
    <Style TargetType="DataGrid">
        <Setter Property="Background"              Value="{StaticResource BackgroundPanelBrush}"/>
        <Setter Property="Foreground"              Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush"             Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="BorderThickness"         Value="1"/>
        <Setter Property="GridLinesVisibility"     Value="Horizontal"/>
        <Setter Property="HorizontalGridLinesBrush" Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="VerticalGridLinesBrush"  Value="Transparent"/>
        <Setter Property="RowBackground"           Value="{StaticResource BackgroundPanelBrush}"/>
        <Setter Property="AlternatingRowBackground" Value="{StaticResource BackgroundAltBrush}"/>
        <Setter Property="ColumnHeaderHeight"      Value="32"/>
        <Setter Property="RowHeight"               Value="32"/>
        <Setter Property="FontFamily"              Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="FontSize"                Value="12"/>
        <Setter Property="SelectionMode"           Value="Single"/>
        <Setter Property="SelectionUnit"           Value="FullRow"/>
        <Setter Property="CanUserResizeRows"       Value="False"/>
        <Setter Property="HeadersVisibility"       Value="Column"/>
    </Style>

    <!-- ═══ DataGridColumnHeader ═══ -->
    <Style TargetType="DataGridColumnHeader">
        <Setter Property="Background"      Value="{StaticResource BackgroundChromeBrush}"/>
        <Setter Property="Foreground"      Value="{StaticResource TextHeaderBrush}"/>
        <Setter Property="FontSize"        Value="10"/>
        <Setter Property="FontWeight"      Value="SemiBold"/>
        <Setter Property="Padding"         Value="8,0"/>
        <Setter Property="Height"          Value="32"/>
        <Setter Property="BorderBrush"     Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="BorderThickness" Value="0,0,0,1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="DataGridColumnHeader">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            Padding="{TemplateBinding Padding}">
                        <TextBlock Text="{TemplateBinding Content}"
                                   Foreground="{TemplateBinding Foreground}"
                                   FontSize="{TemplateBinding FontSize}"
                                   FontWeight="{TemplateBinding FontWeight}"
                                   VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ═══ DataGridRow ═══ -->
    <Style TargetType="DataGridRow">
        <Setter Property="Background" Value="{StaticResource BackgroundPanelBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="{StaticResource AccentBgBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ═══ DataGridCell ═══ -->
    <Style TargetType="DataGridCell">
        <Setter Property="Background"      Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding"         Value="8,0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="DataGridCell">
                    <Border Background="{TemplateBinding Background}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter VerticalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="Transparent"/>
                <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ═══ DataGridRowHeader (hidden) ═══ -->
    <Style TargetType="DataGridRowHeader">
        <Setter Property="Width" Value="0"/>
    </Style>

    <!-- ═══ ProgressBar ═══ -->
    <Style TargetType="ProgressBar">
        <Setter Property="Background"      Value="{StaticResource BackgroundChromeBrush}"/>
        <Setter Property="Foreground"      Value="{StaticResource AccentBrush}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Height"          Value="6"/>
    </Style>

    <!-- ═══ CheckBox ═══ -->
    <Style TargetType="CheckBox">
        <Setter Property="Foreground"  Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="FontFamily"  Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="FontSize"    Value="12"/>
    </Style>

    <!-- ═══ ListBox ═══ -->
    <Style TargetType="ListBox">
        <Setter Property="Background"      Value="{StaticResource BackgroundPanelBrush}"/>
        <Setter Property="Foreground"      Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush"     Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="FontFamily"      Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="FontSize"        Value="12"/>
    </Style>

    <Style TargetType="ListBoxItem">
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="Padding"    Value="4,2"/>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="{StaticResource AccentBgBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ═══ TreeView ═══ -->
    <Style TargetType="TreeView">
        <Setter Property="Background"  Value="{StaticResource BackgroundPanelBrush}"/>
        <Setter Property="Foreground"  Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BorderSubtleBrush}"/>
        <Setter Property="FontFamily"  Value="Segoe UI Variable, Segoe UI"/>
        <Setter Property="FontSize"    Value="12"/>
    </Style>

    <Style TargetType="TreeViewItem">
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Style.Triggers>
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="Background" Value="{StaticResource AccentBgBrush}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- ═══ GridSplitter ═══ -->
    <Style TargetType="GridSplitter">
        <Setter Property="Background" Value="{StaticResource BorderSubtleBrush}"/>
    </Style>
```

- [ ] **Step 2: Build**

```
dotnet build ADTool/ADTool.csproj
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add ADTool/Themes/Theme.xaml
git commit -m "feat: add DataGrid, TextBox, and remaining control styles to Theme.xaml"
```

---

### Task 3: App.xaml + MainWindow — wire up the theme

**Files:**
- Modify: `ADTool/App.xaml`
- Modify: `ADTool/Views/MainWindow.xaml`

- [ ] **Step 1: Replace App.xaml content**

```xml
<!-- ADTool/App.xaml -->
<Application x:Class="ADTool.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes/Theme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Add Background to MainWindow**

In `ADTool/Views/MainWindow.xaml`, add `Background="{StaticResource BackgroundDeepBrush}"` to the `<Window>` element:

```xml
<Window x:Class="ADTool.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:ADTool.ViewModels"
        xmlns:views="clr-namespace:ADTool.Views"
        Title="{Binding WindowTitle}"
        Height="640" Width="960"
        MinHeight="480" MinWidth="720"
        WindowStartupLocation="CenterScreen"
        Background="{StaticResource BackgroundDeepBrush}">

    <Window.Resources>
        <DataTemplate DataType="{x:Type vm:HomeViewModel}">
            <views:HomeView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:UPNToolViewModel}">
            <views:UPNToolView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:AttributeToolViewModel}">
            <views:AttrToolView />
        </DataTemplate>
    </Window.Resources>

    <ContentControl Content="{Binding CurrentView}"/>
</Window>
```

- [ ] **Step 3: Build and run — verify app opens with dark background and dark buttons on home screen**

```
dotnet build ADTool/ADTool.csproj
dotnet run --project ADTool/ADTool.csproj
```
Expected: App opens, dark background visible, default buttons styled dark.

- [ ] **Step 4: Commit**

```
git add ADTool/App.xaml ADTool/Views/MainWindow.xaml
git commit -m "feat: merge Theme.xaml into App.xaml, set dark window background"
```

---

### Task 4: ViewModel additions — CurrentStepNumber + ReturnHomeCommand

**Files:**
- Modify: `ADTool/ViewModels/UPNToolViewModel.cs`
- Modify: `ADTool/ViewModels/AttributeToolViewModel.cs`
- Modify: `ADTool.Tests/` (add test file)

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/ViewModels/ToolViewModelNavigationTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ToolViewModelNavigationTests"
```
Expected: 6 tests fail — `CurrentStepNumber` and `ReturnHomeCommand` don't exist yet.

- [ ] **Step 3: Update UPNToolViewModel**

In `ADTool/ViewModels/UPNToolViewModel.cs`, make the following changes:

1. Change the `CurrentStep` setter to also fire `CurrentStepNumber` change:
```csharp
public BaseViewModel CurrentStep
{
    get => _currentStep;
    private set
    {
        SetField(ref _currentStep, value);
        OnPropertyChanged(nameof(CurrentStepNumber));
    }
}
```

2. Add after the `CurrentStep` property:
```csharp
public int CurrentStepNumber => Array.IndexOf(_steps, _currentStep) + 1;

public RelayCommand ReturnHomeCommand { get; }
```

3. In the constructor, add before the closing brace:
```csharp
ReturnHomeCommand = new RelayCommand(returnHome);
```

- [ ] **Step 4: Update AttributeToolViewModel**

In `ADTool/ViewModels/AttributeToolViewModel.cs`, apply the same pattern:

```csharp
public class AttributeToolViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries = new();
    private BaseViewModel _currentStep;
    private readonly BaseViewModel[] _steps;
    private readonly AttrStep3PreviewViewModel _step3;

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set
        {
            SetField(ref _currentStep, value);
            OnPropertyChanged(nameof(CurrentStepNumber));
        }
    }

    public int CurrentStepNumber => Array.IndexOf(_steps, _currentStep) + 1;

    public RelayCommand ReturnHomeCommand { get; }

    public AttributeToolViewModel(IAdService adService, Action returnHome)
    {
        var step1 = new AttrStep1InputViewModel(_entries, adService, () => GoTo(2));
        var step2 = new AttrStep2ValidateViewModel(_entries, adService, () => GoTo(1), () => GoTo(3));
        _step3   = new AttrStep3PreviewViewModel(_entries, () => GoTo(2), () => GoTo(4));
        var step4 = new AttrStep4ExecuteViewModel(_entries, adService, Reset);

        _steps = [step1, step2, _step3, step4];
        _currentStep = step1;

        ReturnHomeCommand = new RelayCommand(returnHome);

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
        if (stepNumber == 3)
            _step3.Refresh();
        CurrentStep = _steps[stepNumber - 1];
    }
}
```

- [ ] **Step 5: Run tests — confirm 6 pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ToolViewModelNavigationTests"
```
Expected: 6 tests pass.

- [ ] **Step 6: Commit**

```
git add ADTool/ViewModels/UPNToolViewModel.cs ADTool/ViewModels/AttributeToolViewModel.cs ADTool.Tests/ViewModels/ToolViewModelNavigationTests.cs
git commit -m "feat: add CurrentStepNumber and ReturnHomeCommand to tool ViewModels"
```

---

### Task 5: StepIndicatorControl UserControl

**Files:**
- Create: `ADTool/Views/StepIndicatorControl.xaml`
- Create: `ADTool/Views/StepIndicatorControl.xaml.cs`
- Modify: `ADTool.Tests/` (add test file)

- [ ] **Step 1: Write failing tests for the build logic**

Create `ADTool.Tests/Views/StepIndicatorControlTests.cs`:

```csharp
using ADTool.Views;
using Xunit;

namespace ADTool.Tests.Views;

public class StepIndicatorControlTests
{
    private static readonly string[] Labels = ["Input", "Validate", "Preview", "Execute"];

    [Fact]
    public void BuildItems_Step1Active_AllOthersPending()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.Equal(StepState.Active,  items[0].State);
        Assert.Equal(StepState.Pending, items[1].State);
        Assert.Equal(StepState.Pending, items[2].State);
        Assert.Equal(StepState.Pending, items[3].State);
    }

    [Fact]
    public void BuildItems_Step2Active_Step1Completed()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 2);
        Assert.Equal(StepState.Completed, items[0].State);
        Assert.Equal(StepState.Active,    items[1].State);
        Assert.Equal(StepState.Pending,   items[2].State);
    }

    [Fact]
    public void BuildItems_LastItemHasNoConnector()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.False(items[3].ShowConnector);
        Assert.True(items[0].ShowConnector);
    }

    [Fact]
    public void BuildItems_LabelsAndNumbersCorrect()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.Equal("Input",    items[0].Label);
        Assert.Equal("Validate", items[1].Label);
        Assert.Equal("1",        items[0].Number);
        Assert.Equal("2",        items[1].Number);
    }

    [Fact]
    public void BuildItems_NullSteps_ReturnsEmpty()
    {
        var items = StepIndicatorControl.BuildItems(null, 1);
        Assert.Empty(items);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "StepIndicatorControlTests"
```
Expected: compilation error — `StepIndicatorControl`, `StepState`, `BuildItems` not defined yet.

- [ ] **Step 3: Create StepIndicatorControl.xaml.cs**

```csharp
// ADTool/Views/StepIndicatorControl.xaml.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public enum StepState { Pending, Active, Completed }

public class StepDisplayItem
{
    public string Label        { get; init; } = "";
    public string Number       { get; init; } = "";
    public StepState State     { get; init; }
    public bool ShowConnector  { get; init; }
}

public partial class StepIndicatorControl : UserControl
{
    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(nameof(Steps), typeof(IReadOnlyList<string>),
            typeof(StepIndicatorControl), new PropertyMetadata(null, OnStateChanged));

    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int),
            typeof(StepIndicatorControl), new PropertyMetadata(1, OnStateChanged));

    public IReadOnlyList<string>? Steps
    {
        get => (IReadOnlyList<string>?)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    private readonly ObservableCollection<StepDisplayItem> _displayItems = [];
    public ObservableCollection<StepDisplayItem> DisplayItems => _displayItems;

    public StepIndicatorControl() => InitializeComponent();

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StepIndicatorControl c) c.Rebuild();
    }

    private void Rebuild()
    {
        _displayItems.Clear();
        foreach (var item in BuildItems(Steps, CurrentStep))
            _displayItems.Add(item);
    }

    internal static IReadOnlyList<StepDisplayItem> BuildItems(
        IReadOnlyList<string>? steps, int currentStep)
    {
        if (steps == null || steps.Count == 0) return [];
        return steps.Select((label, i) => new StepDisplayItem
        {
            Label         = label,
            Number        = (i + 1).ToString(),
            State         = (i + 1) < currentStep ? StepState.Completed
                          : (i + 1) == currentStep ? StepState.Active
                          : StepState.Pending,
            ShowConnector = i < steps.Count - 1
        }).ToList();
    }
}
```

- [ ] **Step 4: Create StepIndicatorControl.xaml**

```xml
<!-- ADTool/Views/StepIndicatorControl.xaml -->
<UserControl x:Class="ADTool.Views.StepIndicatorControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:ADTool.Views">
    <Border Background="{StaticResource BackgroundPanelBrush}"
            BorderBrush="{StaticResource BorderSubtleBrush}"
            BorderThickness="0,0,0,1"
            Padding="16,11">
        <ItemsControl ItemsSource="{Binding DisplayItems,
                                   RelativeSource={RelativeSource AncestorType=local:StepIndicatorControl}}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" VerticalAlignment="Center"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate DataType="{x:Type local:StepDisplayItem}">
                    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                        <!-- Circle + label -->
                        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                            <Border Width="22" Height="22" CornerRadius="11" Margin="0,0,6,0">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Setter Property="Background"      Value="#2A2A2C"/>
                                        <Setter Property="BorderBrush"     Value="{StaticResource BorderStrongBrush}"/>
                                        <Setter Property="BorderThickness" Value="1.5"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Active}">
                                                <Setter Property="Background"      Value="{StaticResource AccentBrush}"/>
                                                <Setter Property="BorderThickness" Value="0"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Completed}">
                                                <Setter Property="Background"  Value="{StaticResource SuccessBgBrush}"/>
                                                <Setter Property="BorderBrush" Value="{StaticResource SuccessBrush}"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center"
                                           FontSize="10" FontWeight="Bold">
                                    <TextBlock.Style>
                                        <Style TargetType="TextBlock">
                                            <Setter Property="Text"       Value="{Binding Number}"/>
                                            <Setter Property="Foreground" Value="{StaticResource TextDimBrush}"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Active}">
                                                    <Setter Property="Foreground" Value="#1A1A1A"/>
                                                </DataTrigger>
                                                <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Completed}">
                                                    <Setter Property="Text"       Value="✓"/>
                                                    <Setter Property="FontSize"   Value="9"/>
                                                    <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </TextBlock.Style>
                                </TextBlock>
                            </Border>
                            <TextBlock VerticalAlignment="Center" FontSize="11">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock">
                                        <Setter Property="Text"       Value="{Binding Label}"/>
                                        <Setter Property="Foreground" Value="{StaticResource TextDimBrush}"/>
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Active}">
                                                <Setter Property="Foreground"  Value="{StaticResource AccentBrush}"/>
                                                <Setter Property="FontWeight"  Value="SemiBold"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding State}" Value="{x:Static local:StepState.Completed}">
                                                <Setter Property="Foreground" Value="{StaticResource TextDimBrush}"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </StackPanel>
                        <!-- Connector line between steps -->
                        <Rectangle Width="28" Height="1" Margin="8,0"
                                   Fill="{StaticResource BorderSubtleBrush}"
                                   VerticalAlignment="Center">
                            <Rectangle.Style>
                                <Style TargetType="Rectangle">
                                    <Setter Property="Visibility" Value="Visible"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding ShowConnector}" Value="False">
                                            <Setter Property="Visibility" Value="Collapsed"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </Rectangle.Style>
                        </Rectangle>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Border>
</UserControl>
```

- [ ] **Step 5: Run tests**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "StepIndicatorControlTests"
```
Expected: 5 tests pass.

- [ ] **Step 6: Build**

```
dotnet build ADTool/ADTool.csproj
```
Expected: 0 errors.

- [ ] **Step 7: Commit**

```
git add ADTool/Views/StepIndicatorControl.xaml ADTool/Views/StepIndicatorControl.xaml.cs ADTool.Tests/Views/StepIndicatorControlTests.cs
git commit -m "feat: add StepIndicatorControl with progress pill UI and unit tests"
```

---

### Task 6: HomeView restyle

**Files:**
- Modify: `ADTool/Views/HomeView.xaml`

- [ ] **Step 1: Replace HomeView.xaml content**

```xml
<UserControl x:Class="ADTool.Views.HomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="{StaticResource BackgroundDeepBrush}">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" MinWidth="560">
            <TextBlock Text="AD Tool"
                       FontSize="22" FontWeight="Bold" FontFamily="Segoe UI Variable, Segoe UI"
                       Foreground="#E0E0E0"
                       HorizontalAlignment="Center" Margin="0,0,0,6"/>
            <TextBlock Text="Choose a tool to get started"
                       FontSize="12" FontFamily="Segoe UI Variable, Segoe UI"
                       Foreground="{StaticResource TextMutedBrush}"
                       HorizontalAlignment="Center" Margin="0,0,0,32"/>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                <!-- UPN Modifier card -->
                <Border Width="260" Margin="0,0,16,0"
                        Background="{StaticResource BackgroundPanelBrush}"
                        BorderBrush="{StaticResource BorderSubtleBrush}"
                        BorderThickness="1,3,1,1"
                        CornerRadius="8" Padding="20">
                    <Border.Resources>
                        <!-- Override top border colour via a gradient trick using BorderBrush -->
                    </Border.Resources>
                    <StackPanel>
                        <Border Width="32" Height="32" CornerRadius="6"
                                Background="{StaticResource AccentBgBrush}"
                                HorizontalAlignment="Left" Margin="0,0,0,12">
                            <Rectangle Width="16" Height="2" Fill="{StaticResource AccentBrush}"
                                       HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Text="UPN Bulk Modifier"
                                   FontSize="13" FontWeight="SemiBold"
                                   FontFamily="Segoe UI Variable, Segoe UI"
                                   Foreground="#E0E0E0" Margin="0,0,0,6"/>
                        <TextBlock Text="Change user UPNs and proxy addresses in bulk"
                                   TextWrapping="Wrap"
                                   FontFamily="Segoe UI Variable, Segoe UI"
                                   FontSize="11" Foreground="{StaticResource TextMutedBrush}"
                                   Margin="0,0,0,16"/>
                        <Button Content="Launch →"
                                Command="{Binding LaunchUPNModifierCommand}"
                                Style="{StaticResource PrimaryButtonStyle}"
                                HorizontalAlignment="Left"/>
                    </StackPanel>
                </Border>

                <!-- Attribute Editor card -->
                <Border Width="260"
                        Background="{StaticResource BackgroundPanelBrush}"
                        BorderBrush="{StaticResource BorderSubtleBrush}"
                        BorderThickness="1,3,1,1"
                        CornerRadius="8" Padding="20">
                    <StackPanel>
                        <Border Width="32" Height="32" CornerRadius="6"
                                Background="{StaticResource AccentBgBrush}"
                                HorizontalAlignment="Left" Margin="0,0,0,12">
                            <Border Width="14" Height="14"
                                    BorderBrush="{StaticResource AccentBrush}"
                                    BorderThickness="2" CornerRadius="2"
                                    HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Text="Attribute Editor"
                                   FontSize="13" FontWeight="SemiBold"
                                   FontFamily="Segoe UI Variable, Segoe UI"
                                   Foreground="#E0E0E0" Margin="0,0,0,6"/>
                        <TextBlock Text="Bulk-set Department, cloud attributes, and other AD fields"
                                   TextWrapping="Wrap"
                                   FontFamily="Segoe UI Variable, Segoe UI"
                                   FontSize="11" Foreground="{StaticResource TextMutedBrush}"
                                   Margin="0,0,0,16"/>
                        <Button Content="Launch →"
                                Command="{Binding LaunchAttributeEditorCommand}"
                                Style="{StaticResource PrimaryButtonStyle}"
                                HorizontalAlignment="Left"/>
                    </StackPanel>
                </Border>
            </StackPanel>
        </StackPanel>
    </Grid>
</UserControl>
```

Note: WPF `Border` `BorderThickness="1,3,1,1"` with `BorderBrush="{StaticResource AccentBrush}"` gives the blue top accent stripe. However, `BorderBrush` is a single brush for all sides. To achieve a coloured top border only, use a nested border approach — replace the outer `Border` with:

```xml
<Border Width="260" Margin="0,0,16,0" CornerRadius="8"
        Background="{StaticResource AccentBrush}">
    <Border Background="{StaticResource BackgroundPanelBrush}"
            BorderBrush="{StaticResource BorderSubtleBrush}"
            BorderThickness="1"
            CornerRadius="7"
            Margin="0,3,0,0"
            Padding="20">
        <!-- card content here -->
    </Border>
</Border>
```

Use this nested-border approach in the final XAML for both cards to achieve the blue top accent stripe.

The complete HomeView.xaml with nested borders:

```xml
<UserControl x:Class="ADTool.Views.HomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="{StaticResource BackgroundDeepBrush}">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" MinWidth="560">
            <TextBlock Text="AD Tool"
                       FontSize="22" FontWeight="Bold"
                       FontFamily="Segoe UI Variable, Segoe UI"
                       Foreground="#E0E0E0"
                       HorizontalAlignment="Center" Margin="0,0,0,6"/>
            <TextBlock Text="Choose a tool to get started"
                       FontSize="12" FontFamily="Segoe UI Variable, Segoe UI"
                       Foreground="{StaticResource TextMutedBrush}"
                       HorizontalAlignment="Center" Margin="0,0,0,32"/>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">

                <!-- UPN Modifier card -->
                <Border Width="260" Margin="0,0,16,0" CornerRadius="8"
                        Background="{StaticResource AccentBrush}">
                    <Border Background="{StaticResource BackgroundPanelBrush}"
                            BorderBrush="{StaticResource BorderSubtleBrush}"
                            BorderThickness="1" CornerRadius="7"
                            Margin="0,3,0,0" Padding="20">
                        <StackPanel>
                            <Border Width="32" Height="32" CornerRadius="6"
                                    Background="{StaticResource AccentBgBrush}"
                                    HorizontalAlignment="Left" Margin="0,0,0,12">
                                <Rectangle Width="16" Height="2"
                                           Fill="{StaticResource AccentBrush}"
                                           HorizontalAlignment="Center" VerticalAlignment="Center"/>
                            </Border>
                            <TextBlock Text="UPN Bulk Modifier"
                                       FontSize="13" FontWeight="SemiBold"
                                       FontFamily="Segoe UI Variable, Segoe UI"
                                       Foreground="#E0E0E0" Margin="0,0,0,6"/>
                            <TextBlock Text="Change user UPNs and proxy addresses in bulk"
                                       TextWrapping="Wrap" FontSize="11"
                                       FontFamily="Segoe UI Variable, Segoe UI"
                                       Foreground="{StaticResource TextMutedBrush}"
                                       Margin="0,0,0,16"/>
                            <Button Content="Launch →"
                                    Command="{Binding LaunchUPNModifierCommand}"
                                    Style="{StaticResource PrimaryButtonStyle}"
                                    HorizontalAlignment="Left"/>
                        </StackPanel>
                    </Border>
                </Border>

                <!-- Attribute Editor card -->
                <Border Width="260" CornerRadius="8"
                        Background="{StaticResource AccentBrush}">
                    <Border Background="{StaticResource BackgroundPanelBrush}"
                            BorderBrush="{StaticResource BorderSubtleBrush}"
                            BorderThickness="1" CornerRadius="7"
                            Margin="0,3,0,0" Padding="20">
                        <StackPanel>
                            <Border Width="32" Height="32" CornerRadius="6"
                                    Background="{StaticResource AccentBgBrush}"
                                    HorizontalAlignment="Left" Margin="0,0,0,12">
                                <Border Width="14" Height="14"
                                        BorderBrush="{StaticResource AccentBrush}"
                                        BorderThickness="2" CornerRadius="2"
                                        HorizontalAlignment="Center" VerticalAlignment="Center"/>
                            </Border>
                            <TextBlock Text="Attribute Editor"
                                       FontSize="13" FontWeight="SemiBold"
                                       FontFamily="Segoe UI Variable, Segoe UI"
                                       Foreground="#E0E0E0" Margin="0,0,0,6"/>
                            <TextBlock Text="Bulk-set Department, cloud attributes, and other AD fields"
                                       TextWrapping="Wrap" FontSize="11"
                                       FontFamily="Segoe UI Variable, Segoe UI"
                                       Foreground="{StaticResource TextMutedBrush}"
                                       Margin="0,0,0,16"/>
                            <Button Content="Launch →"
                                    Command="{Binding LaunchAttributeEditorCommand}"
                                    Style="{StaticResource PrimaryButtonStyle}"
                                    HorizontalAlignment="Left"/>
                        </StackPanel>
                    </Border>
                </Border>

            </StackPanel>
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Build and verify home screen visually**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```
Expected: Dark home screen with two cards, blue top accent stripe, dark buttons.

- [ ] **Step 3: Commit**

```
git add ADTool/Views/HomeView.xaml
git commit -m "feat: restyle HomeView with dark cards and blue accent stripe"
```

---

### Task 7: Tool shell views — title bar + StepIndicatorControl

**Files:**
- Modify: `ADTool/Views/UPNToolView.xaml`
- Modify: `ADTool/Views/AttrToolView.xaml`

- [ ] **Step 1: Replace UPNToolView.xaml**

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
    </UserControl.Resources>

    <Grid Background="{StaticResource BackgroundDeepBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Title bar -->
        <Border Grid.Row="0"
                Background="{StaticResource BackgroundChromeBrush}"
                BorderBrush="{StaticResource BorderSubtleBrush}"
                BorderThickness="0,0,0,1"
                Padding="16,8">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock FontSize="11" VerticalAlignment="Center" Cursor="Hand">
                    <Hyperlink Command="{Binding ReturnHomeCommand}"
                               Foreground="{StaticResource AccentBrush}"
                               TextDecorations="None">← Home</Hyperlink>
                </TextBlock>
                <Rectangle Width="1" Height="14" Fill="{StaticResource BorderSubtleBrush}" Margin="12,0"/>
                <TextBlock Text="UPN Bulk Modifier"
                           FontSize="12" FontWeight="Medium"
                           FontFamily="Segoe UI Variable, Segoe UI"
                           Foreground="{StaticResource TextPrimaryBrush}"
                           VerticalAlignment="Center"/>
            </StackPanel>
        </Border>

        <!-- Step indicator -->
        <views:StepIndicatorControl Grid.Row="1" CurrentStep="{Binding CurrentStepNumber}">
            <views:StepIndicatorControl.Steps>
                <x:Array Type="{x:Type sys:String}"
                         xmlns:sys="clr-namespace:System;assembly=System.Runtime">
                    <sys:String>Input</sys:String>
                    <sys:String>Validate</sys:String>
                    <sys:String>Preview</sys:String>
                    <sys:String>Execute</sys:String>
                </x:Array>
            </views:StepIndicatorControl.Steps>
        </views:StepIndicatorControl>

        <!-- Step content -->
        <ContentControl Grid.Row="2" Content="{Binding CurrentStep}" Margin="16"/>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace AttrToolView.xaml**

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
    </UserControl.Resources>

    <Grid Background="{StaticResource BackgroundDeepBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Title bar -->
        <Border Grid.Row="0"
                Background="{StaticResource BackgroundChromeBrush}"
                BorderBrush="{StaticResource BorderSubtleBrush}"
                BorderThickness="0,0,0,1"
                Padding="16,8">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock FontSize="11" VerticalAlignment="Center" Cursor="Hand">
                    <Hyperlink Command="{Binding ReturnHomeCommand}"
                               Foreground="{StaticResource AccentBrush}"
                               TextDecorations="None">← Home</Hyperlink>
                </TextBlock>
                <Rectangle Width="1" Height="14" Fill="{StaticResource BorderSubtleBrush}" Margin="12,0"/>
                <TextBlock Text="Attribute Editor"
                           FontSize="12" FontWeight="Medium"
                           FontFamily="Segoe UI Variable, Segoe UI"
                           Foreground="{StaticResource TextPrimaryBrush}"
                           VerticalAlignment="Center"/>
            </StackPanel>
        </Border>

        <!-- Step indicator -->
        <views:StepIndicatorControl Grid.Row="1" CurrentStep="{Binding CurrentStepNumber}">
            <views:StepIndicatorControl.Steps>
                <x:Array Type="{x:Type sys:String}"
                         xmlns:sys="clr-namespace:System;assembly=System.Runtime">
                    <sys:String>Input</sys:String>
                    <sys:String>Validate</sys:String>
                    <sys:String>Preview</sys:String>
                    <sys:String>Execute</sys:String>
                </x:Array>
            </views:StepIndicatorControl.Steps>
        </views:StepIndicatorControl>

        <!-- Step content -->
        <ContentControl Grid.Row="2" Content="{Binding CurrentStep}" Margin="16"/>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and run — verify title bar and step pills appear**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```
Expected: Launch a tool → title bar with "← Home" link and step pills showing step 1 active.

- [ ] **Step 4: Commit**

```
git add ADTool/Views/UPNToolView.xaml ADTool/Views/AttrToolView.xaml
git commit -m "feat: replace step bar with StepIndicatorControl, add title bar with Home link"
```

---

### Task 8: Input step views (Step 1)

**Files:**
- Modify: `ADTool/Views/Step1InputView.xaml`
- Modify: `ADTool/Views/AttrStep1InputView.xaml`

- [ ] **Step 1: Replace Step1InputView.xaml**

```xml
<UserControl x:Class="ADTool.Views.Step1InputView"
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
            <Button Content="📂  Import CSV" Command="{Binding ImportCsvCommand}"
                    Margin="0,0,8,0"/>
            <Button Content="🔍  Browse AD…" Command="{Binding OpenAdBrowserCommand}"
                    Margin="0,0,12,0"/>
            <Rectangle Width="1" Height="20" Fill="{StaticResource BorderSubtleBrush}"
                       VerticalAlignment="Center" Margin="0,0,12,0"/>
            <TextBlock Text="Bulk suffix swap:" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11"
                       Margin="0,0,6,0"/>
            <TextBox Width="160" Text="{Binding OldSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @old.contoso.com" Margin="0,0,4,0"/>
            <TextBlock Text="→" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" Margin="4,0"/>
            <TextBox Width="160" Text="{Binding NewSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @new.contoso.com" Margin="0,0,8,0"/>
            <Button Content="Apply" Command="{Binding ApplySuffixSwapCommand}"/>
        </WrapPanel>

        <!-- Hint -->
        <TextBlock Grid.Row="1"
                   Text="Enter UPN changes below, or import from CSV (OldUPN column required; NewUPN optional)"
                   Foreground="{StaticResource TextMutedBrush}" FontSize="11" Margin="0,0,0,6"/>

        <!-- DataGrid -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="OLD UPN" Binding="{Binding OldUPN, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="NEW UPN" Binding="{Binding NewUPN, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTemplateColumn Header="" Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="✕" Foreground="{StaticResource ErrorBrush}"
                                    Background="Transparent" BorderThickness="0"
                                    Command="{Binding DataContext.DeleteRowCommand,
                                              RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                    CommandParameter="{Binding}"
                                    Cursor="Hand"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Footer -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11">
                <Run Text="{Binding Entries.Count, Mode=OneWay}"/>
                <Run Text=" entries"/>
            </TextBlock>
            <Button Grid.Column="1" Content="+ Add Row" Command="{Binding AddRowCommand}"
                    Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Next: Validate →"
                    Command="{Binding NextCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace AttrStep1InputView.xaml**

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
            <Button Content="📂  Import CSV"    Command="{Binding ImportCsvCommand}"    Margin="0,0,8,0"/>
            <Button Content="🔍  Browse AD…"    Command="{Binding OpenAdBrowserCommand}" Margin="0,0,8,0"/>
            <Button Content="+ Add Column"      Command="{Binding AddColumnCommand}"/>
        </WrapPanel>

        <!-- Hint -->
        <TextBlock Grid.Row="1"
                   Text="Import a CSV (UPN column required) or browse AD to add users. Use 'Add Column' to choose which attributes to set."
                   Foreground="{StaticResource TextMutedBrush}" FontSize="11"
                   Margin="0,0,0,6" TextWrapping="Wrap"/>

        <!-- DataGrid -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding InputTable}"
                  AutoGenerateColumns="True"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"/>

        <!-- Footer -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11">
                <Run Text="{Binding InputTable.Rows.Count, Mode=OneWay}"/>
                <Run Text=" rows"/>
            </TextBlock>
            <Button Grid.Column="1" Content="+ Add Row"
                    Command="{Binding AddRowCommand}" Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Next: Validate →"
                    Command="{Binding NextCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and run, navigate to Step 1 in both tools**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```
Expected: Dark toolbar, dark DataGrid with chrome headers, primary blue Next button.

- [ ] **Step 4: Commit**

```
git add ADTool/Views/Step1InputView.xaml ADTool/Views/AttrStep1InputView.xaml
git commit -m "feat: restyle Step 1 input views"
```

---

### Task 9: Validate step views (Step 2)

**Files:**
- Modify: `ADTool/Views/Step2ValidateView.xaml`
- Modify: `ADTool/Views/AttrStep2ValidateView.xaml`

- [ ] **Step 1: Replace Step2ValidateView.xaml**

```xml
<UserControl x:Class="ADTool.Views.Step2ValidateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:ADTool.Models"
             Loaded="OnLoaded">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Progress banner (visible while validating) -->
        <Border Grid.Row="0" Margin="0,0,0,8" CornerRadius="5" Padding="12,8"
                Background="#1A2A3A" BorderBrush="#2A4A6A" BorderThickness="1"
                Visibility="{Binding IsValidating, Converter={StaticResource BoolToVis}}">
            <StackPanel Orientation="Horizontal">
                <ProgressBar Width="160" VerticalAlignment="Center"
                             Minimum="0"
                             Maximum="{Binding TotalCount, Mode=OneWay}"
                             Value="{Binding ValidatedCount, Mode=OneWay}"
                             Margin="0,0,10,0"/>
                <TextBlock Foreground="#88C8E8" FontSize="11" VerticalAlignment="Center">
                    <Run Text="Validating "/>
                    <Run Text="{Binding ValidatedCount, Mode=OneWay}"/>
                    <Run Text=" / "/>
                    <Run Text="{Binding TotalCount, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>
        </Border>

        <!-- Results DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True">
            <DataGrid.RowStyle>
                <Style TargetType="DataGridRow" BasedOn="{StaticResource {x:Type DataGridRow}}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Valid}">
                            <Setter Property="Background" Value="{StaticResource BackgroundPanelBrush}"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                            <Setter Property="Background" Value="{StaticResource ErrorBgBrush}"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                            <Setter Property="Background" Value="{StaticResource ErrorBgBrush}"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.InvalidDomain}">
                            <Setter Property="Background" Value="{StaticResource ErrorBgBrush}"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </DataGrid.RowStyle>
            <DataGrid.Columns>
                <DataGridTemplateColumn Header="" Width="32">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock HorizontalAlignment="Center" FontSize="13">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Valid}">
                                                <Setter Property="Text"       Value="✔"/>
                                                <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Pending}">
                                                <Setter Property="Text"       Value="…"/>
                                                <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                                                <Setter Property="Text"       Value="✘"/>
                                                <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                                                <Setter Property="Text"       Value="✘"/>
                                                <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.InvalidDomain}">
                                                <Setter Property="Text"       Value="✘"/>
                                                <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="OLD UPN"      Binding="{Binding OldUPN}"          Width="*"/>
                <DataGridTextColumn Header="NEW UPN"      Binding="{Binding NewUPN}"          Width="*"/>
                <DataGridTextColumn Header="DISPLAY NAME" Binding="{Binding DisplayName}"     Width="160"/>
                <DataGridTextColumn Header="STATUS"       Binding="{Binding ValidationStatus}" Width="120"/>
                <DataGridTextColumn Header="ERROR"        Binding="{Binding ErrorTitle}"      Width="160"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Warning banner -->
        <Border Grid.Row="2" CornerRadius="5" Padding="12,8" Margin="0,8,0,0"
                Background="#2A2A1A" BorderBrush="#4A4A1A" BorderThickness="1"
                Visibility="{Binding HasInvalidRows, Converter={StaticResource BoolToVis}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="⚠  Some users were not found or have errors. "
                           Foreground="{StaticResource WarningBrush}" FontSize="11"
                           VerticalAlignment="Center"/>
                <Button Command="{Binding RemoveInvalidRowsCommand}"
                        Style="{StaticResource DangerButtonStyle}"
                        Content="Remove invalid rows" Padding="8,4"/>
            </StackPanel>
        </Border>

        <!-- Navigation -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}"/>
            <Button Grid.Column="2" Content="Next: Preview →"
                    Command="{Binding NextCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace AttrStep2ValidateView.xaml**

Same structure as Step2ValidateView above but with these column differences:

```xml
<DataGridTextColumn Header="UPN"          Binding="{Binding UserUPN}"          Width="*"/>
<DataGridTextColumn Header="DISPLAY NAME" Binding="{Binding DisplayName}"      Width="160"/>
<DataGridTextColumn Header="STATUS"       Binding="{Binding ValidationStatus}" Width="120"/>
<DataGridTextColumn Header="ERROR"        Binding="{Binding ErrorTitle}"       Width="160"/>
```

And in the RowStyle DataTriggers, omit the `InvalidDomain` trigger (AttrStep2 only flags `NotFound` and `DuplicateNewUPN`). Full file:

```xml
<UserControl x:Class="ADTool.Views.AttrStep2ValidateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:ADTool.Models"
             Loaded="OnLoaded">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Margin="0,0,0,8" CornerRadius="5" Padding="12,8"
                Background="#1A2A3A" BorderBrush="#2A4A6A" BorderThickness="1"
                Visibility="{Binding IsValidating, Converter={StaticResource BoolToVis}}">
            <StackPanel Orientation="Horizontal">
                <ProgressBar Width="160" VerticalAlignment="Center"
                             Minimum="0"
                             Maximum="{Binding TotalCount, Mode=OneWay}"
                             Value="{Binding ValidatedCount, Mode=OneWay}"
                             Margin="0,0,10,0"/>
                <TextBlock Foreground="#88C8E8" FontSize="11" VerticalAlignment="Center">
                    <Run Text="Validating "/>
                    <Run Text="{Binding ValidatedCount, Mode=OneWay}"/>
                    <Run Text=" / "/>
                    <Run Text="{Binding TotalCount, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>
        </Border>

        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True">
            <DataGrid.RowStyle>
                <Style TargetType="DataGridRow" BasedOn="{StaticResource {x:Type DataGridRow}}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                            <Setter Property="Background" Value="{StaticResource ErrorBgBrush}"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                            <Setter Property="Background" Value="{StaticResource ErrorBgBrush}"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </DataGrid.RowStyle>
            <DataGrid.Columns>
                <DataGridTemplateColumn Header="" Width="32">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock HorizontalAlignment="Center" FontSize="13">
                                <TextBlock.Style>
                                    <Style TargetType="TextBlock">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Valid}">
                                                <Setter Property="Text"       Value="✔"/>
                                                <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Pending}">
                                                <Setter Property="Text"       Value="…"/>
                                                <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                                                <Setter Property="Text"       Value="✘"/>
                                                <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                                                <Setter Property="Text"       Value="✘"/>
                                                <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="UPN"          Binding="{Binding UserUPN}"          Width="*"/>
                <DataGridTextColumn Header="DISPLAY NAME" Binding="{Binding DisplayName}"      Width="160"/>
                <DataGridTextColumn Header="STATUS"       Binding="{Binding ValidationStatus}" Width="120"/>
                <DataGridTextColumn Header="ERROR"        Binding="{Binding ErrorTitle}"       Width="160"/>
            </DataGrid.Columns>
        </DataGrid>

        <Border Grid.Row="2" CornerRadius="5" Padding="12,8" Margin="0,8,0,0"
                Background="#2A2A1A" BorderBrush="#4A4A1A" BorderThickness="1"
                Visibility="{Binding HasInvalidRows, Converter={StaticResource BoolToVis}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="⚠  Some users were not found or have errors. "
                           Foreground="{StaticResource WarningBrush}" FontSize="11"
                           VerticalAlignment="Center"/>
                <Button Command="{Binding RemoveInvalidRowsCommand}"
                        Style="{StaticResource DangerButtonStyle}"
                        Content="Remove invalid rows" Padding="8,4"/>
            </StackPanel>
        </Border>

        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}"/>
            <Button Grid.Column="2" Content="Next: Preview →"
                    Command="{Binding NextCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and run, navigate to Step 2 in both tools**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```
Expected: Dark validate view with blue progress bar, dark error rows, amber warning banner, styled buttons.

- [ ] **Step 4: Commit**

```
git add ADTool/Views/Step2ValidateView.xaml ADTool/Views/AttrStep2ValidateView.xaml
git commit -m "feat: restyle Step 2 validate views"
```

---

### Task 10: Preview step views (Step 3)

**Files:**
- Modify: `ADTool/Views/Step3PreviewView.xaml`
- Modify: `ADTool/Views/AttrStep3PreviewView.xaml`

- [ ] **Step 1: Replace Step3PreviewView.xaml**

```xml
<UserControl x:Class="ADTool.Views.Step3PreviewView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Info banner -->
        <Border Grid.Row="0" CornerRadius="5" Padding="12,8" Margin="0,0,0,8"
                Background="#1A2A1A" BorderBrush="#2A4A2A" BorderThickness="1">
            <TextBlock Foreground="#88C888" FontSize="11">
                <Run Text="✔  "/>
                <Run Text="{Binding Entries.Count, Mode=OneWay}"/>
                <Run Text=" users ready. Review the changes below — this cannot be undone."/>
            </TextBlock>
        </Border>

        <!-- Preview DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="DISPLAY NAME"      Binding="{Binding DisplayName}"  Width="180"/>
                <DataGridTextColumn Header="OLD UPN"           Binding="{Binding OldUPN}"       Width="*"/>
                <DataGridTextColumn Header="NEW UPN"           Binding="{Binding NewUPN}"       Width="*"/>
                <DataGridTemplateColumn Header="PROXY ADDED"   Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Foreground="{StaticResource AccentBrush}" FontSize="11">
                                <Run Text="smtp:"/>
                                <Run Text="{Binding OldUPN}"/>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTemplateColumn Header="NEW PRIMARY SMTP" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Foreground="{StaticResource SuccessBrush}" FontSize="11">
                                <Run Text="SMTP:"/>
                                <Run Text="{Binding NewUPN}"/>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="MAIL"         Binding="{Binding NewUPN}"      Width="*"/>
                <DataGridTextColumn Header="MAILNICKNAME" Binding="{Binding MailNickname}" Width="150"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Navigation -->
        <Grid Grid.Row="2" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}"/>
            <Button Grid.Column="2" Content="Execute Changes"
                    Command="{Binding ExecuteCommand}"
                    Style="{StaticResource DangerButtonStyle}"
                    FontWeight="Bold"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace AttrStep3PreviewView.xaml**

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
        <Border Grid.Row="0" CornerRadius="5" Padding="12,8" Margin="0,0,0,8"
                Background="#1A2A1A" BorderBrush="#2A4A2A" BorderThickness="1">
            <TextBlock Foreground="#88C888" FontSize="11">
                <Run Text="✔  "/>
                <Run Text="{Binding EntryCount, Mode=OneWay}"/>
                <Run Text=" users ready. Review the attribute changes below — this cannot be undone."/>
            </TextBlock>
        </Border>

        <!-- Preview DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding PreviewTable}"
                  AutoGenerateColumns="True"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"/>

        <!-- Navigation -->
        <Grid Grid.Row="2" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}"/>
            <Button Grid.Column="2" Content="Execute Changes"
                    Command="{Binding NextCommand}"
                    Style="{StaticResource DangerButtonStyle}"
                    FontWeight="Bold"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and verify**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```

- [ ] **Step 4: Commit**

```
git add ADTool/Views/Step3PreviewView.xaml ADTool/Views/AttrStep3PreviewView.xaml
git commit -m "feat: restyle Step 3 preview views"
```

---

### Task 11: Execute step views (Step 4)

**Files:**
- Modify: `ADTool/Views/Step4ExecuteView.xaml`
- Modify: `ADTool/Views/AttrStep4ExecuteView.xaml`

- [ ] **Step 1: Replace Step4ExecuteView.xaml**

```xml
<UserControl x:Class="ADTool.Views.Step4ExecuteView"
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
                        <Border Margin="0,0,0,4" Padding="12,10" CornerRadius="5" BorderThickness="1">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="Background"   Value="{StaticResource BackgroundPanelBrush}"/>
                                    <Setter Property="BorderBrush"  Value="{StaticResource BorderSubtleBrush}"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding ExecutionStatus}"
                                                     Value="{x:Static models:ExecutionStatus.Failed}">
                                            <Setter Property="Background"  Value="{StaticResource ErrorBgBrush}"/>
                                            <Setter Property="BorderBrush" Value="{StaticResource ErrorBorderBrush}"/>
                                        </DataTrigger>
                                        <DataTrigger Binding="{Binding ExecutionStatus}"
                                                     Value="{x:Static models:ExecutionStatus.Pending}">
                                            <Setter Property="Background"  Value="{StaticResource BackgroundChromeBrush}"/>
                                            <Setter Property="BorderBrush" Value="{StaticResource BorderSubtleBrush}"/>
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
                                                        <Setter Property="Text"       Value="✔"/>
                                                        <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                                        <Setter Property="Text"       Value="✘"/>
                                                        <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                                        <Setter Property="Text"       Value="…"/>
                                                        <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </TextBlock.Style>
                                    </TextBlock>
                                    <StackPanel Grid.Column="1">
                                        <TextBlock FontWeight="SemiBold"
                                                   Foreground="{StaticResource TextPrimaryBrush}"
                                                   Text="{Binding DisplayName}"/>
                                        <TextBlock FontSize="11"
                                                   Foreground="{StaticResource TextMutedBrush}">
                                            <Run Text="{Binding OldUPN}"/>
                                            <Run Text=" → "/>
                                            <Run Text="{Binding NewUPN}"/>
                                        </TextBlock>
                                    </StackPanel>
                                    <TextBlock Grid.Column="2" VerticalAlignment="Center" FontSize="11"
                                               Foreground="{StaticResource TextMutedBrush}"
                                               Text="{Binding ExecutionStatus}" Margin="8,0,0,0"/>
                                </Grid>

                                <!-- Error expander -->
                                <Expander Margin="24,6,0,0" Header="{Binding ErrorTitle}"
                                          Foreground="{StaticResource ErrorBrush}">
                                    <Expander.Style>
                                        <Style TargetType="Expander">
                                            <Setter Property="Visibility" Value="Collapsed"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding ExecutionStatus}"
                                                             Value="{x:Static models:ExecutionStatus.Failed}">
                                                    <Setter Property="Visibility" Value="Visible"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Expander.Style>
                                    <Border Background="{StaticResource ErrorBgBrush}"
                                            BorderBrush="{StaticResource ErrorBorderBrush}"
                                            BorderThickness="1"
                                            Padding="10,8" Margin="0,4,0,0" CornerRadius="4">
                                        <TextBlock Text="{Binding ErrorDetail}"
                                                   TextWrapping="Wrap"
                                                   Foreground="{StaticResource TextPrimaryBrush}"
                                                   FontSize="11"/>
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
            <TextBlock Grid.Column="0" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11">
                <Run Text="{Binding SuccessCount, Mode=OneWay}"/>
                <Run Text=" succeeded  ·  "/>
                <Run Text="{Binding FailCount, Mode=OneWay}"/>
                <Run Text=" failed"/>
            </TextBlock>
            <Button Grid.Column="1" Content="Export Results CSV"
                    Command="{Binding ExportResultsCommand}"
                    Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Start New Run"
                    Command="{Binding StartNewRunCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace AttrStep4ExecuteView.xaml**

Same structure as Step4ExecuteView above, with these differences in the results list item template — replace the UPN arrow line with attribute-editor-specific display (UPN only, no arrow):

Replace:
```xml
<TextBlock FontSize="11" Foreground="{StaticResource TextMutedBrush}">
    <Run Text="{Binding OldUPN}"/>
    <Run Text=" → "/>
    <Run Text="{Binding NewUPN}"/>
</TextBlock>
```

With:
```xml
<TextBlock FontSize="11" Foreground="{StaticResource TextMutedBrush}"
           Text="{Binding UserUPN}"/>
```

And in the footer, the `SuccessCount`/`FailCount` text is the same. Full file:

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

        <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding Entries}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Margin="0,0,0,4" Padding="12,10" CornerRadius="5" BorderThickness="1">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="Background"  Value="{StaticResource BackgroundPanelBrush}"/>
                                    <Setter Property="BorderBrush" Value="{StaticResource BorderSubtleBrush}"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                            <Setter Property="Background"  Value="{StaticResource ErrorBgBrush}"/>
                                            <Setter Property="BorderBrush" Value="{StaticResource ErrorBorderBrush}"/>
                                        </DataTrigger>
                                        <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                            <Setter Property="Background"  Value="{StaticResource BackgroundChromeBrush}"/>
                                            <Setter Property="BorderBrush" Value="{StaticResource BorderSubtleBrush}"/>
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
                                                        <Setter Property="Text"       Value="✔"/>
                                                        <Setter Property="Foreground" Value="{StaticResource SuccessBrush}"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                                        <Setter Property="Text"       Value="✘"/>
                                                        <Setter Property="Foreground" Value="{StaticResource ErrorBrush}"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                                        <Setter Property="Text"       Value="…"/>
                                                        <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </TextBlock.Style>
                                    </TextBlock>
                                    <StackPanel Grid.Column="1">
                                        <TextBlock FontWeight="SemiBold"
                                                   Foreground="{StaticResource TextPrimaryBrush}"
                                                   Text="{Binding DisplayName}"/>
                                        <TextBlock FontSize="11"
                                                   Foreground="{StaticResource TextMutedBrush}"
                                                   Text="{Binding UserUPN}"/>
                                    </StackPanel>
                                    <TextBlock Grid.Column="2" VerticalAlignment="Center" FontSize="11"
                                               Foreground="{StaticResource TextMutedBrush}"
                                               Text="{Binding ExecutionStatus}" Margin="8,0,0,0"/>
                                </Grid>
                                <Expander Margin="24,6,0,0" Header="{Binding ErrorTitle}"
                                          Foreground="{StaticResource ErrorBrush}">
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
                                    <Border Background="{StaticResource ErrorBgBrush}"
                                            BorderBrush="{StaticResource ErrorBorderBrush}"
                                            BorderThickness="1"
                                            Padding="10,8" Margin="0,4,0,0" CornerRadius="4">
                                        <TextBlock Text="{Binding ErrorDetail}"
                                                   TextWrapping="Wrap"
                                                   Foreground="{StaticResource TextPrimaryBrush}"
                                                   FontSize="11"/>
                                    </Border>
                                </Expander>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <Grid Grid.Row="1" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11">
                <Run Text="{Binding SuccessCount, Mode=OneWay}"/>
                <Run Text=" succeeded  ·  "/>
                <Run Text="{Binding FailCount, Mode=OneWay}"/>
                <Run Text=" failed"/>
            </TextBlock>
            <Button Grid.Column="1" Content="Export Results CSV"
                    Command="{Binding ExportResultsCommand}"
                    Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Start New Run"
                    Command="{Binding StartNewRunCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and verify**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```

- [ ] **Step 4: Commit**

```
git add ADTool/Views/Step4ExecuteView.xaml ADTool/Views/AttrStep4ExecuteView.xaml
git commit -m "feat: restyle Step 4 execute views"
```

---

### Task 12: Dialogs — AdBrowserDialog + AddColumnDialog

**Files:**
- Modify: `ADTool/Views/AdBrowserDialog.xaml`
- Modify: `ADTool/Views/AddColumnDialog.xaml`

- [ ] **Step 1: Replace AdBrowserDialog.xaml**

```xml
<Window x:Class="ADTool.Views.AdBrowserDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:models="clr-namespace:ADTool.Models"
        Title="Browse Active Directory" Height="520" Width="780"
        WindowStartupLocation="CenterOwner"
        ResizeMode="CanResize"
        Background="{StaticResource BackgroundDeepBrush}">

    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Main panel: OU tree + user list -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="220" MinWidth="140"/>
                <ColumnDefinition Width="5"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Left: OU tree -->
            <Grid Grid.Column="0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="Organisational Units"
                           FontWeight="SemiBold" FontSize="11"
                           Foreground="{StaticResource TextMutedBrush}"
                           Margin="0,0,0,6"/>
                <Grid Grid.Row="1">
                    <TreeView x:Name="OuTreeView" ItemsSource="{Binding OuNodes}"
                              SelectedItemChanged="OnOuSelected">
                        <TreeView.ItemTemplate>
                            <HierarchicalDataTemplate DataType="{x:Type models:OuNode}"
                                                      ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Name}" Padding="2,2"/>
                            </HierarchicalDataTemplate>
                        </TreeView.ItemTemplate>
                    </TreeView>
                    <TextBlock Text="Loading…"
                               Foreground="{StaticResource TextMutedBrush}"
                               HorizontalAlignment="Center" VerticalAlignment="Center"
                               Visibility="{Binding IsLoadingTree, Converter={StaticResource BoolToVis}}"/>
                </Grid>
            </Grid>

            <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch"/>

            <!-- Right: user list -->
            <Grid Grid.Column="2" Margin="8,0,0,0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="Users (all levels of selected OU)"
                           FontWeight="SemiBold" FontSize="11"
                           Foreground="{StaticResource TextMutedBrush}"
                           Margin="0,0,0,6"/>
                <Grid Grid.Row="1">
                    <DataGrid ItemsSource="{Binding Users}"
                              AutoGenerateColumns="False"
                              CanUserAddRows="False"
                              CanUserDeleteRows="False"
                              IsReadOnly="False"
                              SelectionMode="Single">
                        <DataGrid.Columns>
                            <DataGridCheckBoxColumn Header="✓" Width="32"
                                Binding="{Binding IsSelected, UpdateSourceTrigger=PropertyChanged}"/>
                            <DataGridTextColumn Header="UPN"          Binding="{Binding UPN}"         Width="*"   IsReadOnly="True"/>
                            <DataGridTextColumn Header="DISPLAY NAME" Binding="{Binding DisplayName}" Width="180" IsReadOnly="True"/>
                        </DataGrid.Columns>
                    </DataGrid>
                    <TextBlock Text="Loading users…"
                               HorizontalAlignment="Center" VerticalAlignment="Center"
                               Foreground="{StaticResource TextMutedBrush}"
                               Visibility="{Binding IsLoadingUsers, Converter={StaticResource BoolToVis}}"/>
                    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center"
                               Foreground="{StaticResource TextMutedBrush}" FontStyle="Italic">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Setter Property="Text"       Value="Select an OU on the left to view users"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding SelectedOu}" Value="{x:Null}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>
                </Grid>
            </Grid>
        </Grid>

        <!-- Button row -->
        <Grid Grid.Row="1" Margin="0,10,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11">
                <Run Text="{Binding Users.Count, Mode=OneWay}"/>
                <Run Text=" users in selected OU"/>
            </TextBlock>
            <Button Grid.Column="1" Content="Add Selected to List"
                    Command="{Binding AddSelectedToListCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Export to CSV"
                    Command="{Binding ExportToCsvCommand}"
                    Margin="0,0,8,0"/>
            <Button Grid.Column="3" Content="Cancel" Click="OnCancel"/>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: Replace AddColumnDialog.xaml**

```xml
<Window x:Class="ADTool.Views.AddColumnDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Add Columns" Width="380" Height="480"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        Background="{StaticResource BackgroundDeepBrush}">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0"
                   Text="Select attributes to add as columns:"
                   FontWeight="SemiBold" FontSize="12"
                   Foreground="{StaticResource TextPrimaryBrush}"
                   Margin="0,0,0,8"/>

        <ListBox Grid.Row="1" x:Name="AttributeList" Margin="0,0,0,12"
                 BorderBrush="{StaticResource BorderSubtleBrush}" BorderThickness="1">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <CheckBox IsChecked="{Binding IsChecked}"
                              Content="{Binding DisplayName}"
                              Padding="4,2"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,16">
            <TextBlock Text="Custom LDAP name:" VerticalAlignment="Center"
                       Foreground="{StaticResource TextMutedBrush}" FontSize="11"
                       Margin="0,0,8,0"/>
            <TextBox x:Name="CustomLdapTextBox" Width="190"
                     ToolTip="e.g. msDS-cloudExtensionAttribute1"/>
        </StackPanel>

        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="OK" Click="OkButton_Click" Width="80"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Margin="0,0,8,0" IsDefault="True"/>
            <Button Content="Cancel" IsCancel="True" Width="80"/>
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 3: Build and run — open Browse AD and Add Column dialogs and verify dark styling**

```
dotnet build ADTool/ADTool.csproj && dotnet run --project ADTool/ADTool.csproj
```
Expected: Both dialogs open with dark backgrounds, dark TreeView/ListBox, styled buttons.

- [ ] **Step 4: Commit**

```
git add ADTool/Views/AdBrowserDialog.xaml ADTool/Views/AddColumnDialog.xaml
git commit -m "feat: restyle AdBrowserDialog and AddColumnDialog"
```

---

### Task 13: Final build, test run, and version bump

**Files:**
- Modify: `ADTool/ADTool.csproj` (version)

- [ ] **Step 1: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```
Expected: All tests pass.

- [ ] **Step 2: Full build in Release mode**

```
dotnet build ADTool/ADTool.csproj -c Release
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Run the app end-to-end**

```
dotnet run --project ADTool/ADTool.csproj
```
Manually verify:
- Home screen shows dark cards with blue accent stripe
- Launch UPN Modifier → title bar shows "← Home", step pills show step 1 active
- Navigate through all 4 steps in dry-run mode — pills update correctly
- ← Home link returns to home screen
- Launch Attribute Editor → same verification
- Open Browse AD dialog — dark theme
- Open Add Column dialog — dark theme

- [ ] **Step 4: Commit and tag**

```
git add -A
git commit -m "chore: complete dark theme UI redesign"
```
