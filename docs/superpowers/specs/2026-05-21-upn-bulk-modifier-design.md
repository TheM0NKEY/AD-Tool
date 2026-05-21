# UPN Bulk Modifier — Design Spec

**Date:** 2026-05-21  
**Status:** Approved

---

## Overview

A WPF desktop tool for bulk-modifying Active Directory user UPNs. After changing a UPN, the tool promotes the new UPN to primary SMTP proxy address and demotes the old UPN to a secondary proxy address to preserve email continuity. All changes are validated and previewed before execution.

---

## Architecture

- **Framework:** WPF, .NET 8, single self-contained `.exe`
- **Pattern:** MVVM — one ViewModel per wizard step
- **AD access:**
  - `System.DirectoryServices.AccountManagement` — user lookup, UPN writes
  - `System.DirectoryServices.DirectoryEntry` — `proxyAddresses` attribute manipulation
- **CSV:** CsvHelper (NuGet)
- **Tests:** xUnit + Moq, separate test project

### Project Structure

```
ADTool/
├── ADTool.csproj
├── App.xaml
├── Models/
│   └── UPNChangeEntry.cs
├── Services/
│   ├── IAdService.cs
│   ├── AdService.cs
│   ├── AdServiceStub.cs          # dry-run stub
│   └── CsvImportService.cs
├── ViewModels/
│   ├── MainViewModel.cs          # wizard orchestration
│   ├── Step1InputViewModel.cs
│   ├── Step2ValidateViewModel.cs
│   ├── Step3PreviewViewModel.cs
│   └── Step4ExecuteViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   ├── Step1InputView.xaml
│   ├── Step2ValidateView.xaml
│   ├── Step3PreviewView.xaml
│   └── Step4ExecuteView.xaml
└── ADTool.Tests/
    ├── ADTool.Tests.csproj
    ├── CsvImportServiceTests.cs
    ├── ProxyAddressLogicTests.cs
    ├── UPNChangeEntryTests.cs
    ├── ViewModelStepGateTests.cs
    └── ErrorCategorizationTests.cs
```

---

## Data Model

`UPNChangeEntry` is the single shared record that flows through all wizard steps. All four ViewModels bind to the same `ObservableCollection<UPNChangeEntry>`.

```csharp
public class UPNChangeEntry : INotifyPropertyChanged
{
    public string OldUPN { get; set; }
    public string NewUPN { get; set; }
    public string? DisplayName { get; set; }        // populated during Step 2
    public ValidationStatus ValidationStatus { get; set; }
    public ExecutionStatus ExecutionStatus { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorDetail { get; set; }
}

public enum ValidationStatus { Pending, Valid, NotFound, DuplicateNewUPN, InvalidDomain }
public enum ExecutionStatus  { Pending, Success, Failed }
```

---

## Wizard Steps

### Step 1 — Input

- **DataGrid** bound to `ObservableCollection<UPNChangeEntry>` with inline editing and per-row delete
- **Import CSV** button — opens file dialog, parses via `CsvImportService`, appends rows to the collection. Expected columns: `OldUPN`, `NewUPN`
- **Bulk suffix swap** bar — two text fields (`Old suffix`, `New suffix`) and an Apply button that rewrites the `NewUPN` of every row by replacing the old suffix
- **Next** button enabled only when the collection has at least one entry

### Step 2 — Validate

On entry, runs `IAdService.ValidateUsersAsync()` against all `Pending` rows concurrently (with a progress indicator):

1. `UserPrincipal.FindByIdentity()` — confirms user exists, populates `DisplayName`
2. LDAP search for `NewUPN` — confirms it is not already assigned to another object
3. Checks that the `NewUPN` suffix is a registered UPN suffix in the forest

Results displayed in a DataGrid with a status icon per row. Invalid rows highlighted. A **Remove invalid rows** button strips all non-`Valid` entries. **Next** is disabled if any `Invalid` rows remain.

### Step 3 — Preview

Read-only summary DataGrid showing, per user:

| Display Name | Old UPN → New UPN | Proxy address added | Primary SMTP updated |
|---|---|---|---|
| John Smith | jsmith@old.com → jsmith@new.com | smtp:jsmith@old.com | SMTP:jsmith@new.com |

Confirmation banner: "X users will be modified. This cannot be undone." **Execute** button is styled distinctly (red accent) to signal irreversibility.

### Step 4 — Execute

Runs changes sequentially (not in parallel — avoids AD rate limiting). Per user:

1. Set `userPrincipalName = NewUPN` via `UserPrincipal.Save()`
2. Via `DirectoryEntry` on the same user object:
   - Find `SMTP:OldUPN` in `proxyAddresses`, demote to `smtp:OldUPN`
   - Add `SMTP:NewUPN` as new primary
   - All other proxy addresses are left unchanged

Results shown in a DataGrid. Failed rows display an inline error card (see Error Handling). Summary line: `X succeeded · Y failed`.

Footer actions: **Export Results CSV** (writes all rows with status and error details), **Start New Run** (resets to Step 1).

---

## AD Operations — `IAdService`

```csharp
public interface IAdService
{
    Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn);
    Task<ExecutionResult> UpdateUserAsync(UPNChangeEntry entry);
}
```

`AdService` (live) and `AdServiceStub` (dry-run, activated via `--dry-run` CLI flag) both implement this interface.

---

## Proxy Address Logic

Given a user whose `proxyAddresses` before the change is:

```
SMTP:jsmith@old.com
smtp:jsmith@alias.com
```

After `UpdateUserAsync`:

```
SMTP:jsmith@new.com        ← new primary
smtp:jsmith@old.com        ← old primary demoted
smtp:jsmith@alias.com      ← untouched
```

Rules:
- Exactly one entry may have the `SMTP:` (uppercase) prefix — the primary
- The old primary is lowercased in-place (prefix changed to `smtp:`)
- The new primary is inserted with uppercase `SMTP:` prefix
- No other entries are modified or removed

---

## Error Handling

Errors are caught per-user and assigned a named type. The UI shows `ErrorTitle` as a bold heading and `ErrorDetail` as a paragraph. A collapsible section exposes the raw exception message for troubleshooting.

| Error Type | Title | Plain-English Detail |
|---|---|---|
| `UserNotFound` | User not found | No user with this UPN exists in Active Directory. Check for typos or verify the domain suffix. |
| `DuplicateNewUPN` | UPN already in use | The new UPN is already assigned to another user. Choose a different UPN. |
| `InsufficientPermissions` | Insufficient permissions | Your account doesn't have permission to modify this user. You need Write access to `userPrincipalName` and `proxyAddresses` on the target OU, or run this tool as a Domain Admin. |
| `ProxyAddressConflict` | Proxy address conflict | The old UPN already exists as a proxy address on another AD object. Manual cleanup is required before this entry can be processed. |
| `InvalidDomain` | Unknown UPN suffix | The new UPN suffix is not a registered UPN suffix in this forest. Add it in Active Directory Domains and Trusts first. |
| `UnexpectedError` | Unexpected error | An unexpected error occurred. See the technical details below. |

`UserNotFound`, `DuplicateNewUPN`, and `InvalidDomain` are caught during Step 2 validation. `InsufficientPermissions`, `ProxyAddressConflict`, and `UnexpectedError` can only surface during Step 4 execution.

---

## CSV Import

Expected format:

```csv
OldUPN,NewUPN
jsmith@old.com,jsmith@new.com
awhite@old.com,awhite@new.com
```

`CsvImportService` validates:
- Required columns `OldUPN` and `NewUPN` present (case-insensitive)
- Neither field is blank on any row
- No duplicate `OldUPN` values within the import batch or against existing rows

Malformed rows are skipped and reported in a non-blocking import summary dialog.

---

## Unit Tests

Test project: `ADTool.Tests` (xUnit + Moq)

| Test Class | Covers |
|---|---|
| `CsvImportServiceTests` | Valid CSV, missing columns, blank fields, duplicates, malformed rows |
| `ProxyAddressLogicTests` | Primary demotion, new primary insertion, preservation of existing secondaries, user with no existing primary |
| `UPNChangeEntryTests` | Status enum transitions, property change notifications |
| `ViewModelStepGateTests` | Next/Execute button enabled state for each step boundary condition |
| `ErrorCategorizationTests` | Correct `ErrorTitle`/`ErrorDetail` assigned for each exception type thrown by mocked `IAdService` |

`IAdService` is mocked via Moq in all ViewModel and error categorization tests. No live AD connection required.

---

## Dry-Run Mode

Launch with `--dry-run` to swap in `AdServiceStub`. The stub:
- Returns `Valid` for all users during validation
- Logs intended changes to the results grid without modifying AD
- Useful for testing on non-domain machines or demoing the UI flow
