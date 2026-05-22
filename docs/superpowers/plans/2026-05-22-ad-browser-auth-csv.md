# AD Browser, Auth Gate, and OldUPN-Only CSV Import — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add OldUPN-only CSV import, an AD user browser dialog in Step 1, and a Domain Admin auth gate at startup.

**Architecture:** Extend `IAdService` with three new methods (`CheckIsDomainAdminAsync`, `GetOuTreeAsync`, `GetUsersInOuAsync`). `AdServiceStub` gets stub implementations. New `AdBrowserDialog` (WPF Window) is constructed and shown by `Step1InputViewModel.OpenAdBrowserCommand`. Auth check runs in `App.xaml.cs` before the main window is shown.

**Tech Stack:** WPF .NET 8, MVVM, `System.DirectoryServices`, `System.DirectoryServices.AccountManagement`, xunit, Moq

---

### Task 1: CsvImportService — OldUPN-only CSV support

**Files:**
- Modify: `ADTool/Services/CsvImportService.cs`
- Modify: `ADTool.Tests/UnitTest1.cs`

- [ ] **Step 1: Replace the placeholder test file with real tests**

Replace the entire content of `ADTool.Tests/UnitTest1.cs`:

```csharp
using ADTool.Services;
using System.IO;
using Xunit;

namespace ADTool.Tests;

public class CsvImportServiceTests
{
    private static string WriteTempCsv(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Import_OldUpnOnly_ReturnsRowsWithEmptyNewUpn()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN\nalice@old.com\nbob@old.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@old.com", result.Rows[0].OldUPN);
        Assert.Equal("", result.Rows[0].NewUPN);
        Assert.Equal("bob@old.com", result.Rows[1].OldUPN);
        Assert.Equal("", result.Rows[1].NewUPN);
    }

    [Fact]
    public void Import_NewUpnColumnPresentButBlank_SkipsRowWithError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN,NewUPN\nalice@old.com,\nbob@old.com,bob@new.com");
        var result = svc.Import(path, []);
        Assert.Single(result.Errors);
        Assert.Single(result.Rows);
        Assert.Equal("bob@old.com", result.Rows[0].OldUPN);
        Assert.Equal("bob@new.com", result.Rows[0].NewUPN);
    }

    [Fact]
    public void Import_MissingOldUpnColumn_ReturnsFileError()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("NewUPN\nbob@new.com");
        var result = svc.Import(path, []);
        Assert.Single(result.Errors);
        Assert.Contains("OldUPN", result.Errors[0]);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_BothColumns_PreservesExistingBehavior()
    {
        var svc = new CsvImportService();
        var path = WriteTempCsv("OldUPN,NewUPN\nalice@old.com,alice@new.com\nbob@old.com,bob@new.com");
        var result = svc.Import(path, []);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("alice@new.com", result.Rows[0].NewUPN);
    }
}
```

- [ ] **Step 2: Run tests to confirm they all fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "FullyQualifiedName~CsvImportServiceTests" -v minimal
```

Expected: 3 of the 4 tests fail (the `BothColumns` test will pass immediately since existing behavior is unchanged).

- [ ] **Step 3: Update `CsvImportService.cs` to make NewUPN optional**

Replace the content of `ADTool/Services/CsvImportService.cs`:

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
            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord ?? [];
            string? oldHeader = headers.FirstOrDefault(h => h.Equals("OldUPN", StringComparison.OrdinalIgnoreCase));
            string? newHeader = headers.FirstOrDefault(h => h.Equals("NewUPN", StringComparison.OrdinalIgnoreCase));

            if (oldHeader is null)
            {
                errors.Add("CSV must contain an 'OldUPN' column.");
                return new CsvImportResult(rows, errors);
            }

            bool hasNewUpnColumn = newHeader is not null;
            var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int rowNum = 1;

            while (csv.Read())
            {
                rowNum++;
                string oldUpn = csv.GetField<string>(oldHeader)?.Trim() ?? string.Empty;
                string newUpn = hasNewUpnColumn
                    ? csv.GetField<string>(newHeader!)?.Trim() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrEmpty(oldUpn))
                {
                    errors.Add($"Row {rowNum}: OldUPN cannot be blank.");
                    continue;
                }
                if (hasNewUpnColumn && string.IsNullOrEmpty(newUpn))
                {
                    errors.Add($"Row {rowNum}: NewUPN cannot be blank.");
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
        catch (FileNotFoundException)
        {
            errors.Add($"File not found: {filePath}");
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read CSV: {ex.Message}");
        }

        return new CsvImportResult(rows, errors);
    }
}
```

- [ ] **Step 4: Run tests to confirm they all pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "FullyQualifiedName~CsvImportServiceTests" -v minimal
```

Expected: 4 tests pass, 0 fail.

- [ ] **Step 5: Commit**

```
git add ADTool/Services/CsvImportService.cs ADTool.Tests/UnitTest1.cs
git commit -m "feat: allow CSV import with OldUPN column only; NewUPN defaults to empty"
```

---

### Task 2: New models for the AD browser

**Files:**
- Create: `ADTool/Models/AdBrowserModels.cs`

- [ ] **Step 1: Create the model file**

Create `ADTool/Models/AdBrowserModels.cs`:

```csharp
namespace ADTool.Models;

public record OuNode(string Name, string DistinguishedName, IReadOnlyList<OuNode> Children);
public record AdUser(string UPN, string DisplayName);
```

- [ ] **Step 2: Verify the project builds**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add ADTool/Models/AdBrowserModels.cs
git commit -m "feat: add OuNode and AdUser models for AD browser"
```

---

### Task 3: Extend IAdService with three new methods

**Files:**
- Modify: `ADTool/Services/IAdService.cs`

- [ ] **Step 1: Add the three new method signatures**

Replace the content of `ADTool/Services/IAdService.cs`:

```csharp
using ADTool.Models;

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
    Task<bool> CheckIsDomainAdminAsync();
    Task<IReadOnlyList<OuNode>> GetOuTreeAsync();
    Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName);
}
```

- [ ] **Step 2: Verify the project fails to build (AdService and AdServiceStub are now incomplete)**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Expected: Errors for `AdService` and `AdServiceStub` not implementing `IAdService`.

---

### Task 4: AdServiceStub — implement the three new methods

**Files:**
- Modify: `ADTool/Services/AdServiceStub.cs`

- [ ] **Step 1: Add stub implementations**

Replace the content of `ADTool/Services/AdServiceStub.cs`:

```csharp
using ADTool.Models;

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

    public Task<bool> CheckIsDomainAdminAsync()
        => Task.FromResult(true);

    public Task<IReadOnlyList<OuNode>> GetOuTreeAsync()
    {
        IReadOnlyList<OuNode> tree =
        [
            new OuNode("contoso.com", "DC=contoso,DC=com",
            [
                new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []),
                new OuNode("IT", "OU=IT,DC=contoso,DC=com",
                [
                    new OuNode("Operations", "OU=Operations,OU=IT,DC=contoso,DC=com", [])
                ])
            ])
        ];
        return Task.FromResult(tree);
    }

    public Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName)
    {
        IReadOnlyList<AdUser> users =
        [
            new AdUser("alice@contoso.com", "Alice Smith"),
            new AdUser("bob@contoso.com", "Bob Jones"),
            new AdUser("carol@contoso.com", "Carol White"),
        ];
        return Task.FromResult(users);
    }
}
```

- [ ] **Step 2: Verify build succeeds (only AdService still missing)**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Expected: Still errors for `AdService` only.

---

### Task 5: AdService — implement the three new AD query methods

**Files:**
- Modify: `ADTool/Services/AdService.cs`

- [ ] **Step 1: Add the three new method implementations**

Replace the content of `ADTool/Services/AdService.cs`:

```csharp
using ADTool.Models;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

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
                var existing = proxies.Count > 0 ? proxies.Cast<string>().ToList() : new List<string>();
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

    public async Task<bool> CheckIsDomainAdminAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.Current;
                var groups = user.GetAuthorizationGroups();
                return groups.Any(g => g.Name.Equals("Domain Admins", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<IReadOnlyList<OuNode>> GetOuTreeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var rootDse = new DirectoryEntry("LDAP://RootDSE");
                string defaultNC = rootDse.Properties["defaultNamingContext"][0]!.ToString()!;
                string rootName = string.Join(".", defaultNC
                    .Split(',')
                    .Where(p => p.TrimStart().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.TrimStart()[3..]));
                var children = GetOuChildren(defaultNC);
                return (IReadOnlyList<OuNode>)[new OuNode(rootName, defaultNC, children)];
            }
            catch
            {
                return Array.Empty<OuNode>();
            }
        });
    }

    public async Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{ouDistinguishedName}");
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(&(objectClass=user)(objectCategory=person)(userPrincipalName=*))",
                    SearchScope = SearchScope.Subtree,
                    PageSize = 1000
                };
                searcher.PropertiesToLoad.AddRange(new[] { "userPrincipalName", "displayName" });

                using var results = searcher.FindAll();
                var users = new List<AdUser>();
                foreach (SearchResult result in results)
                {
                    string upn = result.Properties["userPrincipalName"][0]?.ToString() ?? "";
                    string displayName = result.Properties["displayName"].Count > 0
                        ? result.Properties["displayName"][0]?.ToString() ?? upn
                        : upn;
                    if (!string.IsNullOrEmpty(upn))
                        users.Add(new AdUser(upn, displayName));
                }
                return (IReadOnlyList<AdUser>)users;
            }
            catch
            {
                return Array.Empty<AdUser>();
            }
        });
    }

    private static IReadOnlyList<OuNode> GetOuChildren(string parentDn)
    {
        try
        {
            using var entry = new DirectoryEntry($"LDAP://{parentDn}");
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=organizationalUnit)",
                SearchScope = SearchScope.OneLevel
            };
            searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName" });

            using var results = searcher.FindAll();
            var nodes = new List<OuNode>();
            foreach (SearchResult result in results)
            {
                string dn = result.Properties["distinguishedName"][0]?.ToString() ?? "";
                string name = result.Properties["name"][0]?.ToString() ?? dn;
                nodes.Add(new OuNode(name, dn, GetOuChildren(dn)));
            }
            return nodes;
        }
        catch
        {
            return Array.Empty<OuNode>();
        }
    }

    private static bool IsValidUpnSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return false;
        try
        {
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            string configNC = rootDse.Properties["configurationNamingContext"][0]!.ToString()!;
            string forestRoot = string.Join(".", configNC.Split(',')
                .Where(p => p.TrimStart().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.TrimStart()[3..]));

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

- [ ] **Step 2: Verify the full project builds**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj -v minimal
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```
git add ADTool/Services/IAdService.cs ADTool/Services/AdService.cs ADTool/Services/AdServiceStub.cs ADTool/Models/AdBrowserModels.cs
git commit -m "feat: extend IAdService with CheckIsDomainAdminAsync, GetOuTreeAsync, GetUsersInOuAsync"
```

---

### Task 6: Domain Admin auth gate in App.xaml.cs

**Files:**
- Modify: `ADTool/App.xaml.cs`

- [ ] **Step 1: Make OnStartup async and add the auth check**

Replace the content of `ADTool/App.xaml.cs`:

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

        var mainVm = new MainViewModel(adService, new CsvImportService());
        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}
```

- [ ] **Step 2: Build and run in dry-run mode to confirm the window still opens**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Then launch: `ADTool\bin\Debug\net8.0-windows\win-x64\ADTool.exe --dry-run`

Expected: Main window opens without any auth prompt.

- [ ] **Step 3: Commit**

```
git add ADTool/App.xaml.cs
git commit -m "feat: block non-Domain Admins at startup; bypass in dry-run mode"
```

---

### Task 7: AdBrowserViewModel

**Files:**
- Create: `ADTool/ViewModels/AdBrowserViewModel.cs`
- Create: `ADTool.Tests/AdBrowserViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ADTool.Tests/AdBrowserViewModelTests.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using Xunit;

namespace ADTool.Tests;

public class AdBrowserViewModelTests
{
    private static Mock<IAdService> MakeMock(
        IReadOnlyList<OuNode>? tree = null,
        IReadOnlyList<AdUser>? users = null)
    {
        var mock = new Mock<IAdService>();
        mock.Setup(s => s.GetOuTreeAsync())
            .ReturnsAsync(tree ?? Array.Empty<OuNode>());
        mock.Setup(s => s.GetUsersInOuAsync(It.IsAny<string>()))
            .ReturnsAsync(users ?? Array.Empty<AdUser>());
        mock.Setup(s => s.CheckIsDomainAdminAsync()).ReturnsAsync(true);
        return mock;
    }

    [Fact]
    public async Task LoadTreeAsync_PopulatesOuNodes()
    {
        IReadOnlyList<OuNode> tree = [new OuNode("contoso.com", "DC=contoso,DC=com", [])];
        var vm = new AdBrowserViewModel(MakeMock(tree: tree).Object, _ => { });

        await vm.LoadTreeAsync();

        Assert.Single(vm.OuNodes);
        Assert.Equal("contoso.com", vm.OuNodes[0].Name);
    }

    [Fact]
    public async Task LoadTreeAsync_SetsIsLoadingTreeFalseWhenDone()
    {
        var vm = new AdBrowserViewModel(MakeMock().Object, _ => { });

        await vm.LoadTreeAsync();

        Assert.False(vm.IsLoadingTree);
    }

    [Fact]
    public async Task SettingSelectedOu_LoadsUsersIntoCollection()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;

        Assert.Single(vm.Users);
        Assert.Equal("alice@contoso.com", vm.Users[0].UPN);
        Assert.Equal("Alice Smith", vm.Users[0].DisplayName);
    }

    [Fact]
    public async Task AddSelectedToListCommand_DisabledWhenNoUsersSelected()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;

        Assert.False(vm.AddSelectedToListCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSelectedToListCommand_EnabledWhenUserSelected()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;

        Assert.True(vm.AddSelectedToListCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSelectedToListCommand_PassesSelectedUsersToCallback()
    {
        IReadOnlyList<AdUser> users =
        [
            new AdUser("alice@contoso.com", "Alice Smith"),
            new AdUser("bob@contoso.com", "Bob Jones"),
        ];
        IReadOnlyList<AdUser>? received = null;
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, u => received = u);

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;
        vm.AddSelectedToListCommand.Execute(null);

        Assert.NotNull(received);
        Assert.Single(received);
        Assert.Equal("alice@contoso.com", received[0].UPN);
    }

    [Fact]
    public async Task AddSelectedToListCommand_RaisesRequestClose()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });
        bool closeFired = false;
        vm.RequestClose += (_, _) => closeFired = true;

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;
        vm.AddSelectedToListCommand.Execute(null);

        Assert.True(closeFired);
    }
}
```

- [ ] **Step 2: Run tests to confirm they all fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "FullyQualifiedName~AdBrowserViewModelTests" -v minimal
```

Expected: All 6 tests fail (type not found).

- [ ] **Step 3: Create `AdBrowserViewModel.cs`**

Create `ADTool/ViewModels/AdBrowserViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ADTool.ViewModels;

public class SelectableAdUser : INotifyPropertyChanged
{
    private bool _isSelected;

    public AdUser User { get; }
    public string UPN => User.UPN;
    public string DisplayName => User.DisplayName;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public SelectableAdUser(AdUser user) => User = user;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class AdBrowserViewModel : BaseViewModel
{
    private readonly IAdService _adService;
    private readonly Action<IReadOnlyList<AdUser>> _onAddToList;

    private IReadOnlyList<OuNode> _ouNodes = [];
    private OuNode? _selectedOu;
    private ObservableCollection<SelectableAdUser> _users = [];
    private bool _isLoadingTree;
    private bool _isLoadingUsers;

    public IReadOnlyList<OuNode> OuNodes
    {
        get => _ouNodes;
        private set => SetField(ref _ouNodes, value);
    }

    public OuNode? SelectedOu
    {
        get => _selectedOu;
        set
        {
            SetField(ref _selectedOu, value);
            LatestLoadUsersTask = LoadUsersAsync(value);
        }
    }

    public ObservableCollection<SelectableAdUser> Users
    {
        get => _users;
        private set => SetField(ref _users, value);
    }

    public bool IsLoadingTree
    {
        get => _isLoadingTree;
        private set => SetField(ref _isLoadingTree, value);
    }

    public bool IsLoadingUsers
    {
        get => _isLoadingUsers;
        private set => SetField(ref _isLoadingUsers, value);
    }

    public Task LatestLoadUsersTask { get; private set; } = Task.CompletedTask;

    public RelayCommand AddSelectedToListCommand { get; }
    public RelayCommand ExportToCsvCommand { get; }

    public event EventHandler? RequestClose;

    public AdBrowserViewModel(IAdService adService, Action<IReadOnlyList<AdUser>> onAddToList)
    {
        _adService = adService;
        _onAddToList = onAddToList;
        AddSelectedToListCommand = new RelayCommand(AddSelectedToList, () => _users.Any(u => u.IsSelected));
        ExportToCsvCommand = new RelayCommand(ExportToCsv, () => !_isLoadingTree && _users.Count > 0);
    }

    public async Task LoadTreeAsync()
    {
        IsLoadingTree = true;
        OuNodes = await _adService.GetOuTreeAsync();
        IsLoadingTree = false;
        ExportToCsvCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadUsersAsync(OuNode? ou)
    {
        Users = [];
        AddSelectedToListCommand.RaiseCanExecuteChanged();
        ExportToCsvCommand.RaiseCanExecuteChanged();
        if (ou is null) return;

        IsLoadingUsers = true;
        var rawUsers = await _adService.GetUsersInOuAsync(ou.DistinguishedName);
        var selectable = rawUsers.Select(u =>
        {
            var s = new SelectableAdUser(u);
            s.PropertyChanged += (_, _) => AddSelectedToListCommand.RaiseCanExecuteChanged();
            return s;
        });
        Users = new ObservableCollection<SelectableAdUser>(selectable);
        IsLoadingUsers = false;
        ExportToCsvCommand.RaiseCanExecuteChanged();
    }

    private void AddSelectedToList()
    {
        var selected = _users.Where(u => u.IsSelected).Select(u => u.User).ToList();
        _onAddToList(selected);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void ExportToCsv()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"ad-users-{DateTime.Now:yyyy-MM-dd-HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        using var writer = new StreamWriter(dlg.FileName);
        writer.WriteLine("OldUPN,NewUPN,DisplayName");
        foreach (var u in _users)
            writer.WriteLine($"{u.UPN},,{EscapeCsv(u.DisplayName)}");
    }

    private static string EscapeCsv(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
```

- [ ] **Step 4: Run tests to confirm they all pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "FullyQualifiedName~AdBrowserViewModelTests" -v minimal
```

Expected: All 6 tests pass.

- [ ] **Step 5: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj -v minimal
```

Expected: All tests pass.

- [ ] **Step 6: Commit**

```
git add ADTool/ViewModels/AdBrowserViewModel.cs ADTool.Tests/AdBrowserViewModelTests.cs
git commit -m "feat: add AdBrowserViewModel with OU tree and user list loading"
```

---

### Task 8: AdBrowserDialog — WPF Window

**Files:**
- Create: `ADTool/Views/AdBrowserDialog.xaml`
- Create: `ADTool/Views/AdBrowserDialog.xaml.cs`

- [ ] **Step 1: Create the XAML**

Create `ADTool/Views/AdBrowserDialog.xaml`:

```xml
<Window x:Class="ADTool.Views.AdBrowserDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:models="clr-namespace:ADTool.Models"
        Title="Browse Active Directory" Height="520" Width="780"
        WindowStartupLocation="CenterOwner"
        ResizeMode="CanResize">
    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </Window.Resources>

    <Grid Margin="10">
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
                <TextBlock Grid.Row="0" Text="Organisational Units" FontWeight="SemiBold" Margin="0,0,0,4"/>
                <Grid Grid.Row="1">
                    <TreeView x:Name="OuTreeView" ItemsSource="{Binding OuNodes}"
                              SelectedItemChanged="OnOuSelected">
                        <TreeView.ItemTemplate>
                            <HierarchicalDataTemplate DataType="{x:Type models:OuNode}"
                                                      ItemsSource="{Binding Children}">
                                <TextBlock Text="{Binding Name}" Padding="2,1"/>
                            </HierarchicalDataTemplate>
                        </TreeView.ItemTemplate>
                    </TreeView>
                    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center"
                                Visibility="{Binding IsLoadingTree, Converter={StaticResource BoolToVis}}">
                        <TextBlock Text="Loading..." Foreground="Gray" HorizontalAlignment="Center"/>
                    </StackPanel>
                </Grid>
            </Grid>

            <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch" Background="#DDD"/>

            <!-- Right: user list -->
            <Grid Grid.Column="2" Margin="8,0,0,0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="Users (all levels of selected OU)" FontWeight="SemiBold" Margin="0,0,0,4"/>
                <Grid Grid.Row="1">
                    <DataGrid ItemsSource="{Binding Users}"
                              AutoGenerateColumns="False"
                              CanUserAddRows="False"
                              CanUserDeleteRows="False"
                              HeadersVisibility="Column"
                              IsReadOnly="False"
                              SelectionMode="Single">
                        <DataGrid.Columns>
                            <DataGridCheckBoxColumn Header="✓" Width="32"
                                Binding="{Binding IsSelected, UpdateSourceTrigger=PropertyChanged}"/>
                            <DataGridTextColumn Header="UPN" Binding="{Binding UPN}" Width="*" IsReadOnly="True"/>
                            <DataGridTextColumn Header="Display Name" Binding="{Binding DisplayName}" Width="180" IsReadOnly="True"/>
                        </DataGrid.Columns>
                    </DataGrid>
                    <TextBlock Text="Loading users…" HorizontalAlignment="Center" VerticalAlignment="Center"
                               Foreground="Gray"
                               Visibility="{Binding IsLoadingUsers, Converter={StaticResource BoolToVis}}"/>
                    <TextBlock Text="Select an OU on the left to view users" HorizontalAlignment="Center"
                               VerticalAlignment="Center" Foreground="Gray" FontStyle="Italic">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
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
            <TextBlock Grid.Column="0" VerticalAlignment="Center" Foreground="Gray" FontSize="11">
                <Run Text="{Binding Users.Count, Mode=OneWay}"/>
                <Run Text=" users in selected OU"/>
            </TextBlock>
            <Button Grid.Column="1" Content="Add Selected to List"
                    Command="{Binding AddSelectedToListCommand}"
                    Padding="12,5" Margin="0,0,8,0"
                    Background="#4CAF50" Foreground="White"/>
            <Button Grid.Column="2" Content="Export to CSV"
                    Command="{Binding ExportToCsvCommand}"
                    Padding="12,5" Margin="0,0,8,0"/>
            <Button Grid.Column="3" Content="Cancel" Click="OnCancel" Padding="12,5"/>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: Create the code-behind**

Create `ADTool/Views/AdBrowserDialog.xaml.cs`:

```csharp
using ADTool.Models;
using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AdBrowserDialog : Window
{
    public AdBrowserDialog(AdBrowserViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += (_, _) => Close();
        Loaded += async (_, _) => await vm.LoadTreeAsync();
    }

    private void OnOuSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AdBrowserViewModel vm && e.NewValue is OuNode ou)
            vm.SelectedOu = ou;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 3: Build the project**

```
dotnet build ADTool/ADTool.csproj -v minimal
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```
git add ADTool/Views/AdBrowserDialog.xaml ADTool/Views/AdBrowserDialog.xaml.cs
git commit -m "feat: add AdBrowserDialog WPF window with OU tree and user list"
```

---

### Task 9: Step 1 integration — Browse AD button

**Files:**
- Modify: `ADTool/ViewModels/Step1InputViewModel.cs`
- Modify: `ADTool/ViewModels/MainViewModel.cs`
- Modify: `ADTool/Views/Step1InputView.xaml`

- [ ] **Step 1: Update `Step1InputViewModel.cs` to accept `IAdService` and add `OpenAdBrowserCommand`**

Replace the content of `ADTool/ViewModels/Step1InputViewModel.cs`:

```csharp
using ADTool.Models;
using ADTool.Services;
using ADTool.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace ADTool.ViewModels;

public class Step1InputViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly CsvImportService _csvService;
    private readonly IAdService _adService;
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
    public RelayCommand OpenAdBrowserCommand { get; }
    public RelayCommand ApplySuffixSwapCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand<UPNChangeEntry> DeleteRowCommand { get; }
    public RelayCommand NextCommand { get; }

    public Step1InputViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        CsvImportService csvService,
        IAdService adService,
        Action onNext)
    {
        _entries = entries;
        _csvService = csvService;
        _adService = adService;
        _onNext = onNext;

        ImportCsvCommand = new RelayCommand(ImportCsv);
        OpenAdBrowserCommand = new RelayCommand(OpenAdBrowser);
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

    private void OpenAdBrowser()
    {
        var vm = new AdBrowserViewModel(_adService, AddUsersFromBrowser);
        var dialog = new AdBrowserDialog(vm) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    private void AddUsersFromBrowser(IReadOnlyList<AdUser> users)
    {
        var existingUpns = new HashSet<string>(_entries.Select(e => e.OldUPN), StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            if (existingUpns.Contains(user.UPN)) continue;
            _entries.Add(new UPNChangeEntry { OldUPN = user.UPN, NewUPN = string.Empty });
            existingUpns.Add(user.UPN);
        }
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

- [ ] **Step 2: Update `MainViewModel.cs` to pass `adService` to `Step1InputViewModel`**

Replace the content of `ADTool/ViewModels/MainViewModel.cs`:

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
        var step1 = new Step1InputViewModel(Entries, csvService, adService, () => GoTo(2));
        var step2 = new Step2ValidateViewModel(Entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new Step3PreviewViewModel(Entries, () => GoTo(2), () => GoTo(4));
        var step4 = new Step4ExecuteViewModel(Entries, adService, () => Reset());

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        ResetCommand = new RelayCommand(Reset);
    }

    public void GoTo(int stepNumber)
    {
        if (stepNumber < 1 || stepNumber > _steps.Length)
            throw new ArgumentOutOfRangeException(nameof(stepNumber), $"Step must be between 1 and {_steps.Length}.");
        CurrentStep = _steps[stepNumber - 1];
    }

    private void Reset()
    {
        Entries.Clear();
        GoTo(1);
    }
}
```

- [ ] **Step 3: Add the Browse AD button to `Step1InputView.xaml`**

Replace the content of `ADTool/Views/Step1InputView.xaml`:

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

        <!-- Toolbar: Import + Browse AD + Suffix Swap -->
        <WrapPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8">
            <Button Content="&#x1F4C2;  Import CSV" Command="{Binding ImportCsvCommand}"
                    Padding="10,5" Margin="0,0,8,0"/>
            <Button Content="&#x1F50D;  Browse AD…" Command="{Binding OpenAdBrowserCommand}"
                    Padding="10,5" Margin="0,0,12,0"/>
            <Separator Style="{StaticResource {x:Static ToolBar.SeparatorStyleKey}}" Margin="0,0,12,0"/>
            <TextBlock Text="Bulk suffix swap:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <TextBox Width="160" Text="{Binding OldSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @old.contoso.com" Margin="0,0,4,0"/>
            <TextBlock Text="&#x2192;" VerticalAlignment="Center" Margin="4,0"/>
            <TextBox Width="160" Text="{Binding NewSuffix, UpdateSourceTrigger=PropertyChanged}"
                     ToolTip="e.g. @new.contoso.com" Margin="0,0,8,0"/>
            <Button Content="Apply" Command="{Binding ApplySuffixSwapCommand}" Padding="8,5"/>
        </WrapPanel>

        <!-- Column headers hint -->
        <TextBlock Grid.Row="1" Text="Enter UPN changes below, or import from CSV (OldUPN column required; NewUPN optional)"
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
                            <Button Content="&#x2715;" Foreground="Red" Background="Transparent" BorderThickness="0"
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
            <Button Grid.Column="2" Content="Next: Validate &#x2192;"
                    Command="{Binding NextCommand}"
                    Padding="12,5" Background="#4CAF50" Foreground="White"/>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 4: Build and run full test suite**

```
dotnet build ADTool/ADTool.csproj -v minimal
dotnet test ADTool.Tests/ADTool.Tests.csproj -v minimal
```

Expected: Build succeeded, all tests pass.

- [ ] **Step 5: Smoke-test the full flow in dry-run mode**

Launch `ADTool.exe --dry-run`. Verify:
1. Main window opens (auth check bypassed)
2. Step 1 toolbar has **Browse AD…** button
3. Clicking **Browse AD…** opens the dialog with the stub OU tree (contoso.com → Sales, IT → Operations)
4. Selecting an OU populates the user list with stub users
5. Checking users and clicking **Add Selected to List** adds them to the Step 1 grid with empty NewUPN
6. **Export to CSV** saves a file with `OldUPN,NewUPN,DisplayName` columns
7. Importing a CSV with only `OldUPN` column adds rows with empty NewUPN
8. Applying a suffix swap fills in NewUPN for the imported rows
9. Continuing through to Execute completes successfully

- [ ] **Step 6: Commit**

```
git add ADTool/ViewModels/Step1InputViewModel.cs ADTool/ViewModels/MainViewModel.cs ADTool/Views/Step1InputView.xaml
git commit -m "feat: add Browse AD button to Step 1 with OU tree dialog and direct-add support"
```

---

### Final: push to remote

- [ ] **Push all commits**

```
git push
```
