# Design: AD Browser, Auth Gate, and OldUPN-Only CSV Import

**Date:** 2026-05-22  
**Status:** Approved

---

## Overview

Three features added to the AD UPN Bulk Modifier:

1. **OldUPN-only CSV import** — allow importing a CSV with only the `OldUPN` column; `NewUPN` defaults to empty so users can apply the bulk suffix-swap tool.
2. **AD Browser dialog** — an optional dialog launched from Step 1 that lets users browse the AD OU tree, see users recursively under any OU, and either add them directly to the Step 1 grid or export them to a CSV for external editing.
3. **Domain Admin auth gate** — block non-Domain Admins from running the tool at startup; bypass in `--dry-run` mode.

---

## Architecture

**Pattern:** Extend `IAdService` with three new methods. All AD interaction stays behind the service interface; `AdServiceStub` gets matching stub implementations for dry-run/test.

---

## Feature 1: OldUPN-Only CSV Import

### Scope
Single file change: `Services/CsvImportService.cs`.

### Behaviour
- `NewUPN` column is now **optional** in the import file.
- If the header row contains no `NewUPN` column, every row is imported with `NewUPN = ""`.
- If the column IS present but a cell is blank, that row is still skipped with a row-level error (partial blank is user error).
- The file-level error message changes from _"CSV must contain columns 'OldUPN' and 'NewUPN'"_ to _"CSV must contain an 'OldUPN' column"_.

### CSV formats accepted

```csv
OldUPN
alice@old.contoso.com
bob@old.contoso.com
```

```csv
OldUPN,NewUPN
alice@old.contoso.com,alice@contoso.com
bob@old.contoso.com,
```
— second row above: `NewUPN` column present but blank → row skipped with error.

---

## Feature 2: AD Browser Dialog

### New Models (`Models/AdBrowserModels.cs`)

```csharp
public record OuNode(string Name, string DistinguishedName, IReadOnlyList<OuNode> Children);
public record AdUser(string UPN, string DisplayName);
```

### IAdService Additions

```csharp
Task<IReadOnlyList<OuNode>> GetOuTreeAsync();
Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName);
```

`GetOuTreeAsync` — queries from the domain root, builds a recursive tree of OUs using `DirectorySearcher` with filter `(objectClass=organizationalUnit)`.

`GetUsersInOuAsync` — subtree search from `ouDistinguishedName`, filter `(&(objectClass=user)(objectCategory=person)(userPrincipalName=*))`, loads `userPrincipalName` and `displayName` attributes.

### AdServiceStub Additions

`GetOuTreeAsync` returns a small synthetic tree (e.g. two OUs, each with one child).  
`GetUsersInOuAsync` returns 2–3 synthetic `AdUser` records regardless of the DN passed.

### AdBrowserViewModel (`ViewModels/AdBrowserViewModel.cs`)

| Member | Description |
|--------|-------------|
| `OuNodes` | Root nodes of the OU tree (populated on construction) |
| `SelectedOu` | Currently selected `OuNode`; changing it triggers user list reload |
| `Users` | Flat list of `SelectableAdUser` (wraps `AdUser` with `IsSelected` bool) for the selected OU |
| `IsLoadingTree` | True while `GetOuTreeAsync` is running |
| `IsLoadingUsers` | True while `GetUsersInOuAsync` is running |
| `AddSelectedToListCommand` | Calls `Action<IReadOnlyList<AdUser>>` callback then closes dialog; disabled when no users selected |
| `ExportToCsvCommand` | Opens `SaveFileDialog`, writes `OldUPN,NewUPN,DisplayName` with `NewUPN` blank |

Constructor signature:
```csharp
AdBrowserViewModel(IAdService adService, Action<IReadOnlyList<AdUser>> onAddToList, Action onClose)
```

### AdBrowserDialog (`Views/AdBrowserDialog.xaml`)

A WPF `Window` (modal). Layout:

```
┌─────────────────────────────────────────────────────┐
│  Browse Active Directory                            │
├──────────────┬──────────────────────────────────────┤
│ OU Tree      │ Users in selected OU                 │
│              │  ☐ | UPN                | DisplayName│
│ ▶ contoso.com│  ☑ | alice@contoso.com  | Alice Smith│
│   ▶ Sales    │  ☐ | bob@contoso.com    | Bob Jones  │
│   ▼ IT       │                                      │
│     ▶ Ops    │  [Loading spinner when IsLoadingUsers]│
│              │                                      │
├──────────────┴──────────────────────────────────────┤
│  [Add Selected to List]  [Export to CSV]  [Cancel]  │
└─────────────────────────────────────────────────────┘
```

- Left: `TreeView` with `HierarchicalDataTemplate`, `SelectedItem` two-way bound to `SelectedOu`
- Right: `DataGrid` with checkbox column (`IsSelected`), UPN column, DisplayName column; `IsReadOnly=True` except the checkbox column; progress overlay when `IsLoadingUsers`
- **Add Selected to List** disabled when no users checked
- **Export to CSV** always enabled when tree is loaded

### Step 1 Integration

`Step1InputViewModel` gains:
- `OpenAdBrowserCommand` — constructs `AdBrowserViewModel` with a callback that creates `UPNChangeEntry` objects (OldUPN = user.UPN, NewUPN = "") and adds them to `Entries` (skipping duplicates), then shows the dialog

`Step1InputView.xaml` gains a **Browse AD…** button in the toolbar row, between the existing **Import CSV** and **Add Row** buttons.

---

## Feature 3: Domain Admin Auth Gate

### IAdService Addition

```csharp
Task<bool> CheckIsDomainAdminAsync();
```

### AdService Implementation

Uses `UserPrincipal.Current.GetAuthorizationGroups()`, checks for a group named `"Domain Admins"` (case-insensitive). Any exception returns `false` — fail-closed.

### AdServiceStub Implementation

```csharp
public Task<bool> CheckIsDomainAdminAsync() => Task.FromResult(true);
```

### App.xaml.cs

`OnStartup` becomes `async void`. After constructing the service, if **not** dry-run:

1. `await adService.CheckIsDomainAdminAsync()`
2. If `false`: show `MessageBox` with title _"Access Denied"_ and message _"This tool requires Domain Admin privileges. Your account is not a member of the Domain Admins group."_, call `Shutdown()`, return.
3. If `true`: proceed to construct `MainViewModel` and show `MainWindow` as normal.

In dry-run mode the check is skipped entirely; `MainWindow` is shown immediately.

---

## Files Changed

| File | Change |
|------|--------|
| `Services/IAdService.cs` | Add `CheckIsDomainAdminAsync`, `GetOuTreeAsync`, `GetUsersInOuAsync` |
| `Services/AdService.cs` | Implement all three new methods |
| `Services/AdServiceStub.cs` | Stub all three new methods |
| `Services/CsvImportService.cs` | Make `NewUPN` column optional |
| `Models/AdBrowserModels.cs` | New — `OuNode`, `AdUser` records |
| `ViewModels/AdBrowserViewModel.cs` | New — OU tree + user list logic |
| `Views/AdBrowserDialog.xaml` | New — modal Window |
| `Views/AdBrowserDialog.xaml.cs` | New — code-behind |
| `ViewModels/Step1InputViewModel.cs` | Add `OpenAdBrowserCommand` |
| `Views/Step1InputView.xaml` | Add Browse AD button |
| `App.xaml.cs` | Add async auth check at startup |

---

## Out of Scope

- Paging or search/filter within the user list (can be added later if OUs are large)
- Configuring which group name counts as "admin" (hardcoded to "Domain Admins")
- Remembering the last selected OU between runs
