# UPN Bulk Modifier Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a WPF wizard app that bulk-modifies Active Directory user UPNs, demotes the old UPN to a secondary SMTP proxy address, promotes the new UPN to primary, and validates users before any changes are made.

**Architecture:** Four-step wizard (Input → Validate → Preview → Execute) using MVVM. A shared `ObservableCollection<UPNChangeEntry>` flows through all steps, accumulating validation and execution state. AD operations are isolated behind `IAdService` to keep ViewModels testable.

**Tech Stack:** .NET 8 WPF, C#, `System.DirectoryServices.AccountManagement`, `System.DirectoryServices`, CsvHelper, xUnit, Moq

---

## File Map

```
ADTool.sln
ADTool/
  ADTool.csproj
  App.xaml / App.xaml.cs
  Models/
    UPNChangeEntry.cs
  Services/
    IAdService.cs              (interface + result records + enums)
    AdService.cs               (live AD implementation)
    AdServiceStub.cs           (dry-run stub)
    CsvImportService.cs
    ProxyAddressHelper.cs      (pure proxy address logic, testable)
    ErrorMessages.cs           (maps error types to user-facing strings)
  ViewModels/
    BaseViewModel.cs           (INotifyPropertyChanged + SetField)
    RelayCommand.cs            (ICommand + generic variant)
    MainViewModel.cs           (wizard orchestration + shared collection)
    Step1InputViewModel.cs
    Step2ValidateViewModel.cs
    Step3PreviewViewModel.cs
    Step4ExecuteViewModel.cs
  Views/
    MainWindow.xaml / .cs
    Step1InputView.xaml / .cs
    Step2ValidateView.xaml / .cs
    Step3PreviewView.xaml / .cs
    Step4ExecuteView.xaml / .cs
ADTool.Tests/
  ADTool.Tests.csproj
  CsvImportServiceTests.cs
  ProxyAddressLogicTests.cs
  UPNChangeEntryTests.cs
  RelayCommandTests.cs
  ErrorMessagesTests.cs
  Step1InputViewModelTests.cs
  Step2ValidateViewModelTests.cs
  Step3PreviewViewModelTests.cs
  Step4ExecuteViewModelTests.cs
```

---

## Task 1: Project scaffold

**Files:**
- Create: `ADTool.sln`
- Create: `ADTool/ADTool.csproj`
- Create: `ADTool.Tests/ADTool.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd "C:/Users/jackw/Documents/Programming/AD Tool"
dotnet new sln -n ADTool
dotnet new wpf -n ADTool -o ADTool --framework net8.0-windows
dotnet new xunit -n ADTool.Tests -o ADTool.Tests --framework net8.0-windows
dotnet sln add ADTool/ADTool.csproj
dotnet sln add ADTool.Tests/ADTool.Tests.csproj
```

- [ ] **Step 2: Add NuGet packages to main project**

```bash
dotnet add ADTool/ADTool.csproj package CsvHelper
dotnet add ADTool/ADTool.csproj package System.DirectoryServices
dotnet add ADTool/ADTool.csproj package System.DirectoryServices.AccountManagement
```

- [ ] **Step 3: Configure main project for self-contained single-file output**

Replace the contents of `ADTool/ADTool.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <AssemblyName>ADTool</AssemblyName>
    <RootNamespace>ADTool</RootNamespace>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishSingleFile>true</PublishSingleFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CsvHelper" Version="33.0.1" />
    <PackageReference Include="System.DirectoryServices" Version="8.0.0" />
    <PackageReference Include="System.DirectoryServices.AccountManagement" Version="8.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Configure test project**

Replace the contents of `ADTool.Tests/ADTool.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <ProjectReference Include="..\ADTool\ADTool.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Verify the solution builds**

```bash
dotnet build ADTool.sln
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add ADTool.sln ADTool/ADTool.csproj ADTool.Tests/ADTool.Tests.csproj
git commit -m "feat: scaffold solution with WPF and test projects"
```

---

## Task 2: Data model

**Files:**
- Create: `ADTool/Models/UPNChangeEntry.cs`
- Create: `ADTool.Tests/UPNChangeEntryTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/UPNChangeEntryTests.cs`:

```csharp
using ADTool.Models;
using System.ComponentModel;

namespace ADTool.Tests;

public class UPNChangeEntryTests
{
    [Fact]
    public void PropertyChanged_FiresWhenOldUPNSet()
    {
        var entry = new UPNChangeEntry();
        string? changedProp = null;
        entry.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        entry.OldUPN = "jsmith@old.com";

        Assert.Equal(nameof(UPNChangeEntry.OldUPN), changedProp);
    }

    [Fact]
    public void PropertyChanged_FiresWhenNewUPNSet()
    {
        var entry = new UPNChangeEntry();
        string? changedProp = null;
        entry.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        entry.NewUPN = "jsmith@new.com";

        Assert.Equal(nameof(UPNChangeEntry.NewUPN), changedProp);
    }

    [Fact]
    public void PropertyChanged_FiresWhenValidationStatusSet()
    {
        var entry = new UPNChangeEntry();
        string? changedProp = null;
        entry.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        entry.ValidationStatus = ValidationStatus.Valid;

        Assert.Equal(nameof(UPNChangeEntry.ValidationStatus), changedProp);
    }

    [Fact]
    public void DefaultValidationStatus_IsPending()
    {
        var entry = new UPNChangeEntry();
        Assert.Equal(ValidationStatus.Pending, entry.ValidationStatus);
    }

    [Fact]
    public void DefaultExecutionStatus_IsPending()
    {
        var entry = new UPNChangeEntry();
        Assert.Equal(ExecutionStatus.Pending, entry.ExecutionStatus);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "UPNChangeEntryTests" -v n
```

Expected: FAIL — `UPNChangeEntry` type not found.

- [ ] **Step 3: Implement the data model**

Create `ADTool/Models/UPNChangeEntry.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADTool.Models;

public enum ValidationStatus { Pending, Valid, NotFound, DuplicateNewUPN, InvalidDomain }
public enum ExecutionStatus { Pending, Success, Failed }

public class UPNChangeEntry : INotifyPropertyChanged
{
    private string _oldUpn = string.Empty;
    private string _newUpn = string.Empty;
    private string? _displayName;
    private ValidationStatus _validationStatus;
    private ExecutionStatus _executionStatus;
    private string? _errorTitle;
    private string? _errorDetail;

    public string OldUPN
    {
        get => _oldUpn;
        set { _oldUpn = value; OnPropertyChanged(); }
    }

    public string NewUPN
    {
        get => _newUpn;
        set { _newUpn = value; OnPropertyChanged(); }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "UPNChangeEntryTests" -v n
```

Expected: PASS — 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/Models/UPNChangeEntry.cs ADTool.Tests/UPNChangeEntryTests.cs
git commit -m "feat: add UPNChangeEntry data model with INPC"
```

---

## Task 3: Service interfaces and result types

**Files:**
- Create: `ADTool/Services/IAdService.cs`

No unit tests for this task — it's pure interface/record definitions used by later tasks.

- [ ] **Step 1: Create the service interface file**

Create `ADTool/Services/IAdService.cs`:

```csharp
namespace ADTool.Services;

public enum ValidationType { None, UserNotFound, DuplicateNewUPN, InvalidDomain }
public enum ExecutionErrorType { None, InsufficientPermissions, ProxyAddressConflict, UnexpectedError }

public record ValidationResult(
    bool IsValid,
    string? DisplayName,
    ValidationType FailureType = ValidationType.None,
    string? TechnicalDetail = null);

public record ExecutionResult(
    bool Success,
    ExecutionErrorType ErrorType = ExecutionErrorType.None,
    string? TechnicalDetail = null);

public interface IAdService
{
    Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn);
    Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add ADTool/Services/IAdService.cs
git commit -m "feat: add IAdService interface with result types"
```

---

## Task 4: BaseViewModel and RelayCommand

**Files:**
- Create: `ADTool/ViewModels/BaseViewModel.cs`
- Create: `ADTool/ViewModels/RelayCommand.cs`
- Create: `ADTool.Tests/RelayCommandTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/RelayCommandTests.cs`:

```csharp
using ADTool.ViewModels;

namespace ADTool.Tests;

public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsAction()
    {
        bool called = false;
        var cmd = new RelayCommand(() => called = true);
        cmd.Execute(null);
        Assert.True(called);
    }

    [Fact]
    public void CanExecute_ReturnsTrueWhenNoPredicateGiven()
    {
        var cmd = new RelayCommand(() => { });
        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsFalseWhenPredicateFalse()
    {
        var cmd = new RelayCommand(() => { }, () => false);
        Assert.False(cmd.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_FiresEvent()
    {
        var cmd = new RelayCommand(() => { });
        bool fired = false;
        cmd.CanExecuteChanged += (_, _) => fired = true;
        cmd.RaiseCanExecuteChanged();
        Assert.True(fired);
    }

    [Fact]
    public void GenericRelayCommand_Execute_PassesParameter()
    {
        string? received = null;
        var cmd = new RelayCommand<string>(s => received = s);
        cmd.Execute("hello");
        Assert.Equal("hello", received);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "RelayCommandTests" -v n
```

Expected: FAIL — `RelayCommand` not found.

- [ ] **Step 3: Create BaseViewModel**

Create `ADTool/ViewModels/BaseViewModel.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADTool.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

- [ ] **Step 4: Create RelayCommand**

Create `ADTool/ViewModels/RelayCommand.cs`:

```csharp
using System.Windows.Input;

namespace ADTool.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "RelayCommandTests" -v n
```

Expected: PASS — 5 tests pass.

- [ ] **Step 6: Commit**

```bash
git add ADTool/ViewModels/BaseViewModel.cs ADTool/ViewModels/RelayCommand.cs ADTool.Tests/RelayCommandTests.cs
git commit -m "feat: add BaseViewModel and RelayCommand"
```

---

## Task 5: CsvImportService

**Files:**
- Create: `ADTool/Services/CsvImportService.cs`
- Create: `ADTool.Tests/CsvImportServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/CsvImportServiceTests.cs`:

```csharp
using ADTool.Services;
using System.IO;

namespace ADTool.Tests;

public class CsvImportServiceTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();
    private readonly CsvImportService _svc = new();

    public void Dispose() => File.Delete(_tempFile);

    private void Write(string content) => File.WriteAllText(_tempFile, content);

    [Fact]
    public void Import_ValidCsv_ReturnsRows()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com\nawhite@old.com,awhite@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Equal(2, result.Rows.Count);
        Assert.Empty(result.Errors);
        Assert.Equal("jsmith@old.com", result.Rows[0].OldUPN);
        Assert.Equal("jsmith@new.com", result.Rows[0].NewUPN);
    }

    [Fact]
    public void Import_MissingOldUPNColumn_ReturnsError()
    {
        Write("Source,NewUPN\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("OldUPN", result.Errors[0]);
    }

    [Fact]
    public void Import_BlankField_SkipsRowAndReportsError()
    {
        Write("OldUPN,NewUPN\n,jsmith@new.com\nawhite@old.com,awhite@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Import_DuplicateInFile_SkipsSecondAndReportsError()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com\njsmith@old.com,jsmith@new2.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate", result.Errors[0]);
    }

    [Fact]
    public void Import_DuplicateAgainstExisting_SkipsAndReportsError()
    {
        Write("OldUPN,NewUPN\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, ["jsmith@old.com"]);
        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public void Import_HeadersCaseInsensitive_Works()
    {
        Write("oldupn,newupn\njsmith@old.com,jsmith@new.com");
        var result = _svc.Import(_tempFile, []);
        Assert.Single(result.Rows);
        Assert.Empty(result.Errors);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "CsvImportServiceTests" -v n
```

Expected: FAIL — `CsvImportService` not found.

- [ ] **Step 3: Implement CsvImportService**

Create `ADTool/Services/CsvImportService.cs`:

```csharp
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace ADTool.Services;

public record CsvImportResult(IReadOnlyList<(string OldUPN, string NewUPN)> Rows, IReadOnlyList<string> Errors);

public class CsvImportService
{
    public CsvImportResult Import(string filePath, IEnumerable<string> existingOldUpns)
    {
        var rows = new List<(string OldUPN, string NewUPN)>();
        var errors = new List<string>();
        var existingSet = new HashSet<string>(existingOldUpns, StringComparer.OrdinalIgnoreCase);

        try
        {
            using var reader = new StreamReader(filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord ?? [];
            bool hasOld = headers.Any(h => h.Equals("OldUPN", StringComparison.OrdinalIgnoreCase));
            bool hasNew = headers.Any(h => h.Equals("NewUPN", StringComparison.OrdinalIgnoreCase));

            if (!hasOld || !hasNew)
            {
                errors.Add("CSV must contain columns 'OldUPN' and 'NewUPN'.");
                return new CsvImportResult(rows, errors);
            }

            var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int rowNum = 1;

            while (csv.Read())
            {
                rowNum++;
                string oldUpn = csv.GetField<string>("OldUPN")?.Trim() ?? string.Empty;
                string newUpn = csv.GetField<string>("NewUPN")?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(oldUpn) || string.IsNullOrEmpty(newUpn))
                {
                    errors.Add($"Row {rowNum}: OldUPN and NewUPN cannot be blank.");
                    continue;
                }
                if (seenInBatch.Contains(oldUpn))
                {
                    errors.Add($"Row {rowNum}: Duplicate OldUPN '{oldUpn}' within import file.");
                    continue;
                }
                if (existingSet.Contains(oldUpn))
                {
                    errors.Add($"Row {rowNum}: OldUPN '{oldUpn}' already exists in the current list.");
                    continue;
                }

                seenInBatch.Add(oldUpn);
                rows.Add((oldUpn, newUpn));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read CSV: {ex.Message}");
        }

        return new CsvImportResult(rows, errors);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "CsvImportServiceTests" -v n
```

Expected: PASS — 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/Services/CsvImportService.cs ADTool.Tests/CsvImportServiceTests.cs
git commit -m "feat: add CsvImportService with import validation"
```

---

## Task 6: ProxyAddressHelper

**Files:**
- Create: `ADTool/Services/ProxyAddressHelper.cs`
- Create: `ADTool.Tests/ProxyAddressLogicTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/ProxyAddressLogicTests.cs`:

```csharp
using ADTool.Services;

namespace ADTool.Tests;

public class ProxyAddressLogicTests
{
    [Fact]
    public void DemotesOldPrimaryAndAddsNewPrimary()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.DoesNotContain("SMTP:jsmith@old.com", result);
    }

    [Fact]
    public void PreservesExistingSecondaryAddresses()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com", "smtp:jsmith@other.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@alias.com", result);
        Assert.Contains("smtp:jsmith@other.com", result);
    }

    [Fact]
    public void ExactlyOnePrimaryAfterUpdate()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }

    [Fact]
    public void HandlesNoPrimaryExisting()
    {
        var existing = new[] { "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }

    [Fact]
    public void HandlesEmptyExistingProxyAddresses()
    {
        var result = ProxyAddressHelper.UpdateProxyAddresses([], "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
    }

    [Fact]
    public void MatchIsCaseInsensitiveForOldPrimary()
    {
        var existing = new[] { "SMTP:JSMITH@OLD.COM" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.DoesNotContain(result, a => a.StartsWith("SMTP:JSMITH@OLD", StringComparison.OrdinalIgnoreCase) && a.StartsWith("SMTP:"));
        Assert.Contains("SMTP:jsmith@new.com", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ProxyAddressLogicTests" -v n
```

Expected: FAIL — `ProxyAddressHelper` not found.

- [ ] **Step 3: Implement ProxyAddressHelper**

Create `ADTool/Services/ProxyAddressHelper.cs`:

```csharp
namespace ADTool.Services;

public static class ProxyAddressHelper
{
    public static IReadOnlyList<string> UpdateProxyAddresses(
        IEnumerable<string> existing, string oldUpn, string newUpn)
    {
        var result = new List<string>();
        bool foundPrimary = false;

        foreach (var addr in existing)
        {
            if (addr.Equals($"SMTP:{oldUpn}", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"smtp:{oldUpn}");
                foundPrimary = true;
            }
            else
            {
                result.Add(addr);
            }
        }

        if (!foundPrimary)
            result.Add($"smtp:{oldUpn}");

        result.Add($"SMTP:{newUpn}");
        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ProxyAddressLogicTests" -v n
```

Expected: PASS — 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/Services/ProxyAddressHelper.cs ADTool.Tests/ProxyAddressLogicTests.cs
git commit -m "feat: add ProxyAddressHelper with proxy demotion and promotion logic"
```

---

## Task 7: ErrorMessages

**Files:**
- Create: `ADTool/Services/ErrorMessages.cs`
- Create: `ADTool.Tests/ErrorMessagesTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/ErrorMessagesTests.cs`:

```csharp
using ADTool.Services;

namespace ADTool.Tests;

public class ErrorMessagesTests
{
    [Theory]
    [InlineData(ValidationType.UserNotFound, "User not found")]
    [InlineData(ValidationType.DuplicateNewUPN, "UPN already in use")]
    [InlineData(ValidationType.InvalidDomain, "Unknown UPN suffix")]
    public void ForValidationFailure_ReturnsExpectedTitle(ValidationType type, string expectedTitle)
    {
        var (title, _) = ErrorMessages.ForValidationFailure(type, "old@test.com", "new@test.com");
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void ForValidationFailure_UserNotFound_DetailMentionsOldUpn()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.UserNotFound, "missing@test.com", "new@test.com");
        Assert.Contains("missing@test.com", detail);
    }

    [Fact]
    public void ForValidationFailure_DuplicateNewUPN_DetailMentionsNewUpn()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.DuplicateNewUPN, "old@test.com", "taken@test.com");
        Assert.Contains("taken@test.com", detail);
    }

    [Fact]
    public void ForValidationFailure_InvalidDomain_DetailMentionsSuffix()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.InvalidDomain, "old@test.com", "new@unknown.suffix");
        Assert.Contains("unknown.suffix", detail);
    }

    [Theory]
    [InlineData(ExecutionErrorType.InsufficientPermissions, "Insufficient permissions")]
    [InlineData(ExecutionErrorType.ProxyAddressConflict, "Proxy address conflict")]
    [InlineData(ExecutionErrorType.UnexpectedError, "Unexpected error")]
    public void ForExecutionFailure_ReturnsExpectedTitle(ExecutionErrorType type, string expectedTitle)
    {
        var (title, _) = ErrorMessages.ForExecutionFailure(type, null);
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void ForExecutionFailure_UnexpectedError_IncludesTechnicalDetail()
    {
        var (_, detail) = ErrorMessages.ForExecutionFailure(ExecutionErrorType.UnexpectedError, "Connection refused");
        Assert.Contains("Connection refused", detail);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ErrorMessagesTests" -v n
```

Expected: FAIL — `ErrorMessages` not found.

- [ ] **Step 3: Implement ErrorMessages**

Create `ADTool/Services/ErrorMessages.cs`:

```csharp
namespace ADTool.Services;

public static class ErrorMessages
{
    public static (string Title, string Detail) ForValidationFailure(
        ValidationType type, string oldUpn, string newUpn) => type switch
    {
        ValidationType.UserNotFound => (
            "User not found",
            $"No user with UPN '{oldUpn}' exists in Active Directory. Check for typos or verify the domain suffix."),
        ValidationType.DuplicateNewUPN => (
            "UPN already in use",
            $"The new UPN '{newUpn}' is already assigned to another user. Choose a different UPN."),
        ValidationType.InvalidDomain => (
            "Unknown UPN suffix",
            $"The suffix '@{newUpn.Split('@').LastOrDefault()}' is not a registered UPN suffix in this forest. " +
            "Add it in Active Directory Domains and Trusts first."),
        _ => ("Validation failed", "An unexpected error occurred during validation.")
    };

    public static (string Title, string Detail) ForExecutionFailure(
        ExecutionErrorType type, string? technicalDetail) => type switch
    {
        ExecutionErrorType.InsufficientPermissions => (
            "Insufficient permissions",
            "Your account doesn't have permission to modify this user. You need Write access to " +
            "userPrincipalName and proxyAddresses on the target OU, or run this tool as a Domain Admin."),
        ExecutionErrorType.ProxyAddressConflict => (
            "Proxy address conflict",
            "The old UPN already exists as a proxy address on another AD object. " +
            "Manual cleanup is required before this entry can be processed."),
        _ => (
            "Unexpected error",
            $"An unexpected error occurred. Technical details: {technicalDetail}")
    };
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "ErrorMessagesTests" -v n
```

Expected: PASS — 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/Services/ErrorMessages.cs ADTool.Tests/ErrorMessagesTests.cs
git commit -m "feat: add ErrorMessages with categorised user-facing error strings"
```

---

## Task 8: AdService (live AD)

**Files:**
- Create: `ADTool/Services/AdService.cs`

No unit tests — this class wraps live AD. Tested manually via dry-run mode and a real domain.

- [ ] **Step 1: Implement AdService**

Create `ADTool/Services/AdService.cs`:

```csharp
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text.RegularExpressions;

namespace ADTool.Services;

public class AdService : IAdService
{
    public async Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);

                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, oldUpn);
                if (user is null)
                    return new ValidationResult(false, null, ValidationType.UserNotFound);

                using var duplicate = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, newUpn);
                if (duplicate is not null)
                    return new ValidationResult(false, null, ValidationType.DuplicateNewUPN);

                string newSuffix = newUpn.Contains('@') ? newUpn.Split('@')[1] : string.Empty;
                if (!IsValidUpnSuffix(newSuffix))
                    return new ValidationResult(false, null, ValidationType.InvalidDomain);

                return new ValidationResult(true, user.DisplayName);
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, null, ValidationType.UserNotFound, ex.Message);
            }
        });
    }

    public async Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, oldUpn);

                if (user is null)
                    return new ExecutionResult(false, ExecutionErrorType.UnexpectedError, "User not found at execution time.");

                user.UserPrincipalName = newUpn;
                user.Save();

                var de = (DirectoryEntry)user.GetUnderlyingObject();
                var proxies = de.Properties["proxyAddresses"];
                var existing = proxies.Cast<string>().ToList();
                var updated = ProxyAddressHelper.UpdateProxyAddresses(existing, oldUpn, newUpn);

                proxies.Clear();
                foreach (var addr in updated)
                    proxies.Add(addr);

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

    private static bool IsValidUpnSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return false;
        try
        {
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            string configNC = rootDse.Properties["configurationNamingContext"][0]!.ToString()!;
            string forestRoot = Regex.Replace(configNC, @"^CN=Configuration,", "")
                                     .Replace("DC=", "").Replace(",", ".");

            if (forestRoot.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                return true;

            using var partitions = new DirectoryEntry($"LDAP://CN=Partitions,{configNC}");
            foreach (string s in partitions.Properties["uPNSuffixes"])
                if (s.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
        catch
        {
            return true;
        }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add ADTool/Services/AdService.cs
git commit -m "feat: add AdService for live AD UPN and proxy address updates"
```

---

## Task 9: AdServiceStub and App entry point

**Files:**
- Create: `ADTool/Services/AdServiceStub.cs`
- Modify: `ADTool/App.xaml`
- Modify: `ADTool/App.xaml.cs`

- [ ] **Step 1: Create AdServiceStub**

Create `ADTool/Services/AdServiceStub.cs`:

```csharp
namespace ADTool.Services;

public class AdServiceStub : IAdService
{
    public Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn)
    {
        string displayName = $"[Stub] {oldUpn.Split('@')[0]}";
        return Task.FromResult(new ValidationResult(true, displayName));
    }

    public Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn)
        => Task.FromResult(new ExecutionResult(true));
}
```

- [ ] **Step 2: Remove StartupUri from App.xaml**

Replace the contents of `ADTool/App.xaml` with:

```xml
<Application x:Class="ADTool.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
```

- [ ] **Step 3: Wire up startup and --dry-run in App.xaml.cs**

Replace the contents of `ADTool/App.xaml.cs` with:

```csharp
using ADTool.Services;
using ADTool.ViewModels;
using ADTool.Views;
using System.Windows;

namespace ADTool;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool dryRun = e.Args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        IAdService adService = dryRun ? new AdServiceStub() : new AdService();

        var mainVm = new MainViewModel(adService, new CsvImportService());
        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ADTool/Services/AdServiceStub.cs ADTool/App.xaml ADTool/App.xaml.cs
git commit -m "feat: add AdServiceStub and wire --dry-run flag in App startup"
```

---

## Task 10: MainViewModel

**Files:**
- Create: `ADTool/ViewModels/MainViewModel.cs`
- Create: `ADTool.Tests/Step1InputViewModelTests.cs` (stub — filled in Task 11)

- [ ] **Step 1: Write failing navigation tests**

Create `ADTool.Tests/MainViewModelTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "MainViewModelTests" -v n
```

Expected: FAIL — `MainViewModel` not found.

- [ ] **Step 3: Implement MainViewModel**

Create `ADTool/ViewModels/MainViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentStep;

    public ObservableCollection<UPNChangeEntry> Entries { get; } = new();

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public RelayCommand ResetCommand { get; }

    private readonly BaseViewModel[] _steps;

    public MainViewModel(IAdService adService, CsvImportService csvService)
    {
        var step1 = new Step1InputViewModel(Entries, csvService, () => GoTo(2));
        var step2 = new Step2ValidateViewModel(Entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new Step3PreviewViewModel(Entries, () => GoTo(2), () => GoTo(4));
        var step4 = new Step4ExecuteViewModel(Entries, adService, () => Reset());

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        ResetCommand = new RelayCommand(Reset);
    }

    public void GoTo(int stepNumber) => CurrentStep = _steps[stepNumber - 1];

    private void Reset()
    {
        Entries.Clear();
        GoTo(1);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "MainViewModelTests" -v n
```

Expected: PASS — 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/ViewModels/MainViewModel.cs ADTool.Tests/MainViewModelTests.cs
git commit -m "feat: add MainViewModel with wizard navigation"
```

---

## Task 11: Step1InputViewModel

**Files:**
- Create: `ADTool/ViewModels/Step1InputViewModel.cs`
- Create: `ADTool.Tests/Step1InputViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/Step1InputViewModelTests.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class Step1InputViewModelTests
{
    private ObservableCollection<UPNChangeEntry> Entries() => new();
    private CsvImportService Csv() => new();

    [Fact]
    public void NextCommand_DisabledWhenEntriesEmpty()
    {
        var vm = new Step1InputViewModel(Entries(), Csv(), () => { });
        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void NextCommand_EnabledWhenEntriesHasItems()
    {
        var entries = Entries();
        var vm = new Step1InputViewModel(entries, Csv(), () => { });
        entries.Add(new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com" });
        Assert.True(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public void NextCommand_ResetsPendingValidationStatus()
    {
        var entries = Entries();
        entries.Add(new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com", ValidationStatus = ValidationStatus.Valid });
        bool nextCalled = false;
        var vm = new Step1InputViewModel(entries, Csv(), () => nextCalled = true);

        vm.NextCommand.Execute(null);

        Assert.Equal(ValidationStatus.Pending, entries[0].ValidationStatus);
        Assert.True(nextCalled);
    }

    [Fact]
    public void ApplySuffixSwap_ReplacesMatchingSuffix()
    {
        var entries = Entries();
        entries.Add(new UPNChangeEntry { OldUPN = "jsmith@old.com", NewUPN = "jsmith@old.com" });
        var vm = new Step1InputViewModel(entries, Csv(), () => { });
        vm.OldSuffix = "@old.com";
        vm.NewSuffix = "@new.com";

        vm.ApplySuffixSwapCommand.Execute(null);

        Assert.Equal("jsmith@new.com", entries[0].NewUPN);
    }

    [Fact]
    public void ApplySuffixSwap_DisabledWhenSuffixesEmpty()
    {
        var vm = new Step1InputViewModel(Entries(), Csv(), () => { });
        Assert.False(vm.ApplySuffixSwapCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteRowCommand_RemovesEntry()
    {
        var entries = Entries();
        var entry = new UPNChangeEntry { OldUPN = "a@b.com", NewUPN = "a@c.com" };
        entries.Add(entry);
        var vm = new Step1InputViewModel(entries, Csv(), () => { });

        vm.DeleteRowCommand.Execute(entry);

        Assert.Empty(entries);
    }

    [Fact]
    public void AddRowCommand_AddsBlankEntry()
    {
        var entries = Entries();
        var vm = new Step1InputViewModel(entries, Csv(), () => { });

        vm.AddRowCommand.Execute(null);

        Assert.Single(entries);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step1InputViewModelTests" -v n
```

Expected: FAIL — `Step1InputViewModel` not found.

- [ ] **Step 3: Implement Step1InputViewModel**

Create `ADTool/ViewModels/Step1InputViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace ADTool.ViewModels;

public class Step1InputViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly CsvImportService _csvService;
    private readonly Action _onNext;
    private string _oldSuffix = string.Empty;
    private string _newSuffix = string.Empty;

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

    public string OldSuffix
    {
        get => _oldSuffix;
        set { SetField(ref _oldSuffix, value); ApplySuffixSwapCommand.RaiseCanExecuteChanged(); }
    }

    public string NewSuffix
    {
        get => _newSuffix;
        set { SetField(ref _newSuffix, value); ApplySuffixSwapCommand.RaiseCanExecuteChanged(); }
    }

    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand ApplySuffixSwapCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand<UPNChangeEntry> DeleteRowCommand { get; }
    public RelayCommand NextCommand { get; }

    public Step1InputViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        CsvImportService csvService,
        Action onNext)
    {
        _entries = entries;
        _csvService = csvService;
        _onNext = onNext;

        ImportCsvCommand = new RelayCommand(ImportCsv);
        ApplySuffixSwapCommand = new RelayCommand(ApplySuffixSwap, CanApplySuffixSwap);
        AddRowCommand = new RelayCommand(() => _entries.Add(new UPNChangeEntry()));
        DeleteRowCommand = new RelayCommand<UPNChangeEntry>(e => { if (e != null) _entries.Remove(e); });
        NextCommand = new RelayCommand(Next, () => _entries.Count > 0);

        _entries.CollectionChanged += (_, _) => NextCommand.RaiseCanExecuteChanged();
    }

    private bool CanApplySuffixSwap() =>
        !string.IsNullOrWhiteSpace(_oldSuffix) && !string.IsNullOrWhiteSpace(_newSuffix);

    private void ApplySuffixSwap()
    {
        foreach (var entry in _entries)
            if (entry.OldUPN.EndsWith(_oldSuffix, StringComparison.OrdinalIgnoreCase))
                entry.NewUPN = entry.OldUPN[..^_oldSuffix.Length] + _newSuffix;
    }

    private void ImportCsv()
    {
        var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dlg.ShowDialog() != true) return;

        var existing = _entries.Select(e => e.OldUPN);
        var result = _csvService.Import(dlg.FileName, existing);

        foreach (var (oldUpn, newUpn) in result.Rows)
            _entries.Add(new UPNChangeEntry { OldUPN = oldUpn, NewUPN = newUpn });

        if (result.Errors.Count > 0)
            MessageBox.Show(string.Join("\n", result.Errors), "Import warnings",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Next()
    {
        foreach (var e in _entries)
        {
            e.ValidationStatus = ValidationStatus.Pending;
            e.ErrorTitle = null;
            e.ErrorDetail = null;
        }
        _onNext();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step1InputViewModelTests" -v n
```

Expected: PASS — 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/ViewModels/Step1InputViewModel.cs ADTool.Tests/Step1InputViewModelTests.cs
git commit -m "feat: add Step1InputViewModel with CSV import and suffix swap"
```

---

## Task 12: Step2ValidateViewModel

**Files:**
- Create: `ADTool/ViewModels/Step2ValidateViewModel.cs`
- Create: `ADTool.Tests/Step2ValidateViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ADTool.Tests/Step2ValidateViewModelTests.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using System.Collections.ObjectModel;

namespace ADTool.Tests;

public class Step2ValidateViewModelTests
{
    private static ObservableCollection<UPNChangeEntry> TwoEntries() => new(
    [
        new UPNChangeEntry { OldUPN = "a@old.com", NewUPN = "a@new.com" },
        new UPNChangeEntry { OldUPN = "b@old.com", NewUPN = "b@new.com" }
    ]);

    [Fact]
    public async Task ValidateAllAsync_SetsValidStatusForFoundUsers()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Display Name"));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.Valid, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SetsNotFoundStatus()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.All(entries, e => Assert.Equal(ValidationStatus.NotFound, e.ValidationStatus));
    }

    [Fact]
    public async Task ValidateAllAsync_SetsErrorTitleAndDetail()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = new ObservableCollection<UPNChangeEntry>
        {
            new UPNChangeEntry { OldUPN = "missing@old.com", NewUPN = "missing@new.com" }
        };
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.Equal("User not found", entries[0].ErrorTitle);
        Assert.NotNull(entries[0].ErrorDetail);
    }

    [Fact]
    public async Task NextCommand_DisabledWhenInvalidRowsExist()
    {
        var adMock = new Mock<IAdService>();
        adMock.Setup(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();

        Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public async Task RemoveInvalidRows_RemovesOnlyInvalidEntries()
    {
        var adMock = new Mock<IAdService>();
        adMock.SetupSequence(s => s.ValidateUserAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new ValidationResult(true, "Valid User"))
              .ReturnsAsync(new ValidationResult(false, null, ValidationType.UserNotFound));
        var entries = TwoEntries();
        var vm = new Step2ValidateViewModel(entries, adMock.Object, () => { }, () => { });

        await vm.ValidateAllAsync();
        vm.RemoveInvalidRowsCommand.Execute(null);

        Assert.Single(entries);
        Assert.Equal(ValidationStatus.Valid, entries[0].ValidationStatus);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step2ValidateViewModelTests" -v n
```

Expected: FAIL — `Step2ValidateViewModel` not found.

- [ ] **Step 3: Implement Step2ValidateViewModel**

Create `ADTool/ViewModels/Step2ValidateViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step2ValidateViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isValidating;
    private int _validatedCount;

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

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

    public Step2ValidateViewModel(
        ObservableCollection<UPNChangeEntry> entries,
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

        var tasks = _entries.Select(async entry =>
        {
            var result = await _adService.ValidateUserAsync(entry.OldUPN, entry.NewUPN);
            entry.DisplayName = result.DisplayName;
            entry.ValidationStatus = result.IsValid
                ? ValidationStatus.Valid
                : result.FailureType switch
                {
                    ValidationType.DuplicateNewUPN => ValidationStatus.DuplicateNewUPN,
                    ValidationType.InvalidDomain   => ValidationStatus.InvalidDomain,
                    _                              => ValidationStatus.NotFound
                };

            if (!result.IsValid)
            {
                (entry.ErrorTitle, entry.ErrorDetail) =
                    ErrorMessages.ForValidationFailure(result.FailureType, entry.OldUPN, entry.NewUPN);
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

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step2ValidateViewModelTests" -v n
```

Expected: PASS — 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ADTool/ViewModels/Step2ValidateViewModel.cs ADTool.Tests/Step2ValidateViewModelTests.cs
git commit -m "feat: add Step2ValidateViewModel with concurrent AD validation"
```

---

## Task 13: Step3PreviewViewModel and Step4ExecuteViewModel

**Files:**
- Create: `ADTool/ViewModels/Step3PreviewViewModel.cs`
- Create: `ADTool/ViewModels/Step4ExecuteViewModel.cs`
- Create: `ADTool.Tests/Step4ExecuteViewModelTests.cs`

- [ ] **Step 1: Write failing tests for Step4**

Create `ADTool.Tests/Step4ExecuteViewModelTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step4ExecuteViewModelTests" -v n
```

Expected: FAIL — `Step4ExecuteViewModel` not found.

- [ ] **Step 3: Create Step3PreviewViewModel**

Create `ADTool/ViewModels/Step3PreviewViewModel.cs`:

```csharp
using ADTool.Models;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step3PreviewViewModel : BaseViewModel
{
    public ObservableCollection<UPNChangeEntry> Entries { get; }

    public RelayCommand BackCommand { get; }
    public RelayCommand ExecuteCommand { get; }

    public Step3PreviewViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        Action onBack,
        Action onExecute)
    {
        Entries = entries;
        BackCommand = new RelayCommand(onBack);
        ExecuteCommand = new RelayCommand(onExecute);
    }
}
```

- [ ] **Step 4: Create Step4ExecuteViewModel**

Create `ADTool/ViewModels/Step4ExecuteViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace ADTool.ViewModels;

public class Step4ExecuteViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
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

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

    public RelayCommand ExportResultsCommand { get; }
    public RelayCommand StartNewRunCommand { get; }

    public Step4ExecuteViewModel(
        ObservableCollection<UPNChangeEntry> entries,
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
            var result = await _adService.UpdateUserAsync(entry.OldUPN, entry.NewUPN);
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
            FileName = $"upn-results-{DateTime.Now:yyyy-MM-dd-HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        using var writer = new StreamWriter(dlg.FileName);
        writer.WriteLine("OldUPN,NewUPN,DisplayName,Status,ErrorTitle,ErrorDetail");
        foreach (var e in _entries)
            writer.WriteLine($"{Escape(e.OldUPN)},{Escape(e.NewUPN)},{Escape(e.DisplayName ?? "")}," +
                             $"{e.ExecutionStatus},{Escape(e.ErrorTitle ?? "")},{Escape(e.ErrorDetail ?? "")}");
    }

    private static string Escape(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "Step4ExecuteViewModelTests" -v n
```

Expected: PASS — 3 tests pass.

- [ ] **Step 6: Run all tests**

```bash
dotnet test ADTool.Tests/ADTool.Tests.csproj -v n
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add ADTool/ViewModels/Step3PreviewViewModel.cs ADTool/ViewModels/Step4ExecuteViewModel.cs ADTool.Tests/Step4ExecuteViewModelTests.cs
git commit -m "feat: add Step3PreviewViewModel and Step4ExecuteViewModel"
```

---

## Task 14: MainWindow XAML

**Files:**
- Modify: `ADTool/Views/MainWindow.xaml`
- Modify: `ADTool/Views/MainWindow.xaml.cs`

- [ ] **Step 1: Write MainWindow.xaml**

Replace the contents of `ADTool/Views/MainWindow.xaml` with:

```xml
<Window x:Class="ADTool.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:ADTool.ViewModels"
        xmlns:views="clr-namespace:ADTool.Views"
        Title="AD UPN Bulk Modifier"
        Height="640" Width="960"
        MinHeight="480" MinWidth="720"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
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
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Step indicator bar -->
        <Border Grid.Row="0" Background="#2D2D30" Padding="16,8">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="1  Input" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › " Foreground="#555" Style="{StaticResource StepLabel}"/>
                <TextBlock Text="2  Validate" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › " Foreground="#555" Style="{StaticResource StepLabel}"/>
                <TextBlock Text="3  Preview" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
                <TextBlock Text=" › " Foreground="#555" Style="{StaticResource StepLabel}"/>
                <TextBlock Text="4  Execute" Foreground="#9CDCFE" Style="{StaticResource StepLabel}"/>
            </StackPanel>
        </Border>

        <!-- Active step view (resolved by DataTemplate above) -->
        <ContentControl Grid.Row="1" Content="{Binding CurrentStep}" Margin="16"/>
    </Grid>
</Window>
```

- [ ] **Step 2: Update MainWindow.xaml.cs**

Replace the contents of `ADTool/Views/MainWindow.xaml.cs` with:

```csharp
using System.Windows;

namespace ADTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ADTool/Views/MainWindow.xaml ADTool/Views/MainWindow.xaml.cs
git commit -m "feat: add MainWindow with DataTemplate wizard routing"
```

---

## Task 15: Step1InputView XAML

**Files:**
- Create: `ADTool/Views/Step1InputView.xaml`
- Create: `ADTool/Views/Step1InputView.xaml.cs`

- [ ] **Step 1: Create Step1InputView.xaml**

Create `ADTool/Views/Step1InputView.xaml`:

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

        <!-- Toolbar: Import + Suffix Swap -->
        <WrapPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8">
            <Button Content="📂  Import CSV" Command="{Binding ImportCsvCommand}"
                    Padding="10,5" Margin="0,0,12,0"/>
            <Separator Style="{StaticResource {x:Static ToolBar.SeparatorStyleKey}}" Margin="0,0,12,0"/>
            <TextBlock Text="Bulk suffix swap:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <TextBox Width="160" Text="{Binding OldSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @old.contoso.com" Margin="0,0,4,0"/>
            <TextBlock Text="→" VerticalAlignment="Center" Margin="4,0"/>
            <TextBox Width="160" Text="{Binding NewSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @new.contoso.com" Margin="0,0,8,0"/>
            <Button Content="Apply" Command="{Binding ApplySuffixSwapCommand}" Padding="8,5"/>
        </WrapPanel>

        <!-- Column headers hint -->
        <TextBlock Grid.Row="1" Text="Enter UPN changes below, or import from CSV (columns: OldUPN, NewUPN)"
                   Foreground="Gray" FontSize="11" Margin="0,0,0,4"/>

        <!-- DataGrid -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  HeadersVisibility="Column"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Old UPN" Binding="{Binding OldUPN, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTextColumn Header="New UPN" Binding="{Binding NewUPN, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                <DataGridTemplateColumn Header="" Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="✕" Foreground="Red" Background="Transparent" BorderThickness="0"
                                    Command="{Binding DataContext.DeleteRowCommand,
                                              RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                    CommandParameter="{Binding}"/>
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
            <TextBlock Grid.Column="0" VerticalAlignment="Center" Foreground="Gray" FontSize="11">
                <Run Text="{Binding Entries.Count, Mode=OneWay}"/>
                <Run Text=" entries"/>
            </TextBlock>
            <Button Grid.Column="1" Content="+ Add Row" Command="{Binding AddRowCommand}"
                    Padding="8,5" Margin="0,0,8,0"/>
            <Button Grid.Column="2" Content="Next: Validate →"
                    Command="{Binding NextCommand}"
                    Padding="12,5" Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create Step1InputView.xaml.cs**

Create `ADTool/Views/Step1InputView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step1InputView : UserControl
{
    public Step1InputView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ADTool/Views/Step1InputView.xaml ADTool/Views/Step1InputView.xaml.cs
git commit -m "feat: add Step1InputView with DataGrid, CSV import, and suffix swap"
```

---

## Task 16: Step2ValidateView XAML

**Files:**
- Create: `ADTool/Views/Step2ValidateView.xaml`
- Create: `ADTool/Views/Step2ValidateView.xaml.cs`

- [ ] **Step 1: Create Step2ValidateView.xaml**

Create `ADTool/Views/Step2ValidateView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.Step2ValidateView"
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
                         Minimum="0" Maximum="{Binding TotalCount}" Value="{Binding ValidatedCount}"/>
            <TextBlock Margin="8,0,0,0" VerticalAlignment="Center">
                <Run Text="Validating "/>
                <Run Text="{Binding ValidatedCount, Mode=OneWay}"/>
                <Run Text=" / "/>
                <Run Text="{Binding TotalCount, Mode=OneWay}"/>
            </TextBlock>
        </StackPanel>

        <!-- Results DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Entries}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"
                  HeadersVisibility="Column">
            <DataGrid.RowStyle>
                <Style TargetType="DataGridRow">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                            <Setter Property="Background" Value="#FFF0F0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                            <Setter Property="Background" Value="#FFF0F0"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.InvalidDomain}">
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
                                                <Setter Property="Text" Value="✔"/>
                                                <Setter Property="Foreground" Value="Green"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.Pending}">
                                                <Setter Property="Text" Value="…"/>
                                                <Setter Property="Foreground" Value="Gray"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.NotFound}">
                                                <Setter Property="Text" Value="✘"/>
                                                <Setter Property="Foreground" Value="Red"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.DuplicateNewUPN}">
                                                <Setter Property="Text" Value="✘"/>
                                                <Setter Property="Foreground" Value="Red"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding ValidationStatus}" Value="{x:Static models:ValidationStatus.InvalidDomain}">
                                                <Setter Property="Text" Value="✘"/>
                                                <Setter Property="Foreground" Value="Red"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="Old UPN"      Binding="{Binding OldUPN}"          Width="*"/>
                <DataGridTextColumn Header="New UPN"      Binding="{Binding NewUPN}"          Width="*"/>
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
                <TextBlock Text="⚠  Some users were not found or have errors. " VerticalAlignment="Center"/>
                <Button Content="Remove invalid rows" Command="{Binding RemoveInvalidRowsCommand}"
                        Background="Transparent" BorderThickness="0" Foreground="#856404"
                        TextDecorations="Underline" Cursor="Hand" Padding="0"/>
            </StackPanel>
        </Border>

        <!-- Navigation -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}" Padding="12,5"/>
            <Button Grid.Column="2" Content="Next: Preview →" Command="{Binding NextCommand}"
                    Padding="12,5" Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create Step2ValidateView.xaml.cs**

Create `ADTool/Views/Step2ValidateView.xaml.cs`:

```csharp
using ADTool.ViewModels;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step2ValidateView : UserControl
{
    public Step2ValidateView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is Step2ValidateViewModel vm)
            await vm.ValidateAllAsync();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ADTool/Views/Step2ValidateView.xaml ADTool/Views/Step2ValidateView.xaml.cs
git commit -m "feat: add Step2ValidateView with status icons and invalid row warning"
```

---

## Task 17: Step3PreviewView XAML

**Files:**
- Create: `ADTool/Views/Step3PreviewView.xaml`
- Create: `ADTool/Views/Step3PreviewView.xaml.cs`

- [ ] **Step 1: Create Step3PreviewView.xaml**

Create `ADTool/Views/Step3PreviewView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.Step3PreviewView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Info banner -->
        <Border Grid.Row="0" Background="#E8F5E9" BorderBrush="#4CAF50" BorderThickness="1"
                CornerRadius="3" Padding="10,6" Margin="0,0,0,8">
            <TextBlock>
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
                  IsReadOnly="True"
                  HeadersVisibility="Column">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Display Name"       Binding="{Binding DisplayName}"  Width="180"/>
                <DataGridTextColumn Header="Old UPN"            Binding="{Binding OldUPN}"       Width="*"/>
                <DataGridTextColumn Header="New UPN"            Binding="{Binding NewUPN}"       Width="*"/>
                <DataGridTemplateColumn Header="Proxy Address Added" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Foreground="#1565C0">
                                <Run Text="smtp:"/>
                                <Run Text="{Binding OldUPN}"/>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTemplateColumn Header="New Primary SMTP" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Foreground="#1B5E20">
                                <Run Text="SMTP:"/>
                                <Run Text="{Binding NewUPN}"/>
                            </TextBlock>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Navigation -->
        <Grid Grid.Row="3" Margin="0,8,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding BackCommand}" Padding="12,5"/>
            <Button Grid.Column="2" Content="Execute Changes"
                    Command="{Binding ExecuteCommand}"
                    Padding="16,6" Background="#D32F2F" Foreground="White" FontWeight="Bold"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create Step3PreviewView.xaml.cs**

Create `ADTool/Views/Step3PreviewView.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step3PreviewView : UserControl
{
    public Step3PreviewView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ADTool/Views/Step3PreviewView.xaml ADTool/Views/Step3PreviewView.xaml.cs
git commit -m "feat: add Step3PreviewView with proxy address and SMTP preview columns"
```

---

## Task 18: Step4ExecuteView XAML

**Files:**
- Create: `ADTool/Views/Step4ExecuteView.xaml`
- Create: `ADTool/Views/Step4ExecuteView.xaml.cs`

- [ ] **Step 1: Create Step4ExecuteView.xaml**

Create `ADTool/Views/Step4ExecuteView.xaml`:

```xml
<UserControl x:Class="ADTool.Views.Step4ExecuteView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:ADTool.Models"
             Loaded="OnLoaded">
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </UserControl.Resources>

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
                                        <DataTrigger Binding="{Binding ExecutionStatus}"
                                                     Value="{x:Static models:ExecutionStatus.Failed}">
                                            <Setter Property="Background" Value="#FFF0F0"/>
                                            <Setter Property="BorderBrush" Value="#E57373"/>
                                        </DataTrigger>
                                        <DataTrigger Binding="{Binding ExecutionStatus}"
                                                     Value="{x:Static models:ExecutionStatus.Pending}">
                                            <Setter Property="Background" Value="#FAFAFA"/>
                                            <Setter Property="BorderBrush" Value="#BDBDBD"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </Border.Style>
                            <StackPanel>
                                <!-- Summary row -->
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
                                                        <Setter Property="Text" Value="✔"/>
                                                        <Setter Property="Foreground" Value="Green"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Failed}">
                                                        <Setter Property="Text" Value="✘"/>
                                                        <Setter Property="Foreground" Value="Red"/>
                                                    </DataTrigger>
                                                    <DataTrigger Binding="{Binding ExecutionStatus}" Value="{x:Static models:ExecutionStatus.Pending}">
                                                        <Setter Property="Text" Value="…"/>
                                                        <Setter Property="Foreground" Value="Gray"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </TextBlock.Style>
                                    </TextBlock>
                                    <StackPanel Grid.Column="1">
                                        <TextBlock FontWeight="SemiBold" Text="{Binding DisplayName}"/>
                                        <TextBlock Foreground="Gray" FontSize="11">
                                            <Run Text="{Binding OldUPN}"/>
                                            <Run Text=" → "/>
                                            <Run Text="{Binding NewUPN}"/>
                                        </TextBlock>
                                    </StackPanel>
                                    <TextBlock Grid.Column="2" VerticalAlignment="Center" FontSize="11"
                                               Text="{Binding ExecutionStatus}" Foreground="Gray" Margin="8,0,0,0"/>
                                </Grid>

                                <!-- Error detail expander (shown only on failure) -->
                                <Expander Margin="24,6,0,0" Header="{Binding ErrorTitle}"
                                          Foreground="Red">
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
                                    <Border Background="#FFF3F3" BorderBrush="#FFCDD2"
                                            BorderThickness="0,0,0,0" Padding="8,6" Margin="0,4,0,0"
                                            CornerRadius="2">
                                        <StackPanel>
                                            <TextBlock Text="{Binding ErrorDetail}" TextWrapping="Wrap" Margin="0,0,0,6"/>
                                            <Expander Header="Technical details" FontSize="11" Foreground="Gray">
                                                <TextBlock Text="{Binding ErrorDetail}" FontFamily="Consolas"
                                                           FontSize="10" TextWrapping="Wrap" Foreground="Gray"/>
                                            </Expander>
                                        </StackPanel>
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
                <Run Text=" succeeded  ·  "/>
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

- [ ] **Step 2: Create Step4ExecuteView.xaml.cs**

Create `ADTool/Views/Step4ExecuteView.xaml.cs`:

```csharp
using ADTool.ViewModels;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step4ExecuteView : UserControl
{
    public Step4ExecuteView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is Step4ExecuteViewModel vm)
            await vm.ExecuteAllAsync();
    }
}
```

- [ ] **Step 3: Verify full solution build and all tests pass**

```bash
dotnet build ADTool.sln
dotnet test ADTool.Tests/ADTool.Tests.csproj -v n
```

Expected: `Build succeeded.` All tests pass.

- [ ] **Step 4: Commit**

```bash
git add ADTool/Views/Step4ExecuteView.xaml ADTool/Views/Step4ExecuteView.xaml.cs
git commit -m "feat: add Step4ExecuteView with inline error expanders and export"
```

---

## Task 19: Smoke test with --dry-run

Manual verification — no automated test.

- [ ] **Step 1: Build the app**

```bash
dotnet build ADTool/ADTool.csproj -c Debug
```

Expected: `Build succeeded.`

- [ ] **Step 2: Run with --dry-run**

```bash
dotnet run --project ADTool/ADTool.csproj -- --dry-run
```

Expected: App opens with the wizard UI.

- [ ] **Step 3: Manually verify wizard flow**

Work through this checklist in the running app:

1. **Step 1 — Input**
   - [ ] Click "Add Row", type `jsmith@old.com` in Old UPN and `jsmith@new.com` in New UPN
   - [ ] Enter `@old.com` in old suffix, `@new.com` in new suffix, click Apply — verify NewUPN updates
   - [ ] Click the ✕ button on a row — verify it's removed
   - [ ] Add two rows. Click "Next: Validate →"

2. **Step 2 — Validate**
   - [ ] Validation runs automatically on load; stub returns Valid for all rows
   - [ ] Rows show green ✔ and "Valid" status
   - [ ] "Next: Preview →" is enabled

3. **Step 3 — Preview**
   - [ ] All rows shown with display names, UPN changes, proxy address, and new primary SMTP
   - [ ] Confirmation banner shows count
   - [ ] "Execute Changes" button is red

4. **Step 4 — Execute**
   - [ ] Execution runs automatically on load; stub succeeds silently
   - [ ] All rows show green ✔ and "Success"
   - [ ] Summary shows "2 succeeded · 0 failed"
   - [ ] Click "Export Results CSV" — save dialog opens, CSV is written
   - [ ] Click "Start New Run" — app returns to Step 1 with empty grid

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: complete UPN bulk modifier — all wizard steps wired up"
```
