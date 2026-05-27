# Design: Main Menu and Bulk Attribute Editor

**Date:** 2026-05-27
**Status:** Approved

---

## Overview

The tool gains a home screen that acts as a launcher for two independent functions: the existing UPN Bulk Modifier and a new Attribute Editor. The Attribute Editor lets admins bulk-set a configurable set of AD attributes (Department, Title, extensionAttribute1–15, etc.) across many users in one run, following the same 4-step workflow as the UPN tool.

---

## Navigation

**Home screen (A — home screen pattern).** The app launches to a `HomeView`. Users pick a tool, complete their run, then are returned to the home screen. There is no persistent navigation chrome; switching tools means finishing or abandoning the current run.

**Window title** updates dynamically:
- `"AD Tool"` on the home screen
- `"AD Tool — UPN Modifier"` inside the UPN tool
- `"AD Tool — Attribute Editor"` inside the Attribute Editor

---

## Architecture

`MainViewModel` is renamed/replaced by `AppShellViewModel`. It owns a `CurrentView` property (type `BaseViewModel`) and exposes two launch commands. The step indicator bar moves out of `MainWindow.xaml` and into each tool's own view, since the home screen has no steps.

```
AppShellViewModel
├── CurrentView: BaseViewModel
├── LaunchUPNModifierCommand
└── LaunchAttributeEditorCommand

HomeViewModel          — initial CurrentView
UPNToolViewModel       — wraps the existing 4-step flow
AttributeToolViewModel — new, owns its own 4-step flow
```

### AppShellViewModel

```csharp
public class AppShellViewModel : BaseViewModel
{
    public BaseViewModel CurrentView { get; }
    public void LaunchUPNModifier();
    public void LaunchAttributeEditor();
    public void ReturnHome();
}
```

### UPNToolViewModel

Thin wrapper. Owns `ObservableCollection<UPNChangeEntry>`, a `CurrentStep` property (one of the four step viewmodels), and a `GoTo(int)` method. The only behaviour change: "Start New Run" (Step 4) calls back to `AppShellViewModel.ReturnHome()` instead of resetting to Step 1.

The existing `Step1–4` viewmodels are **unchanged**.

`UPNToolView.xaml` is a new view for `UPNToolViewModel`. It contains the step indicator bar (moved from `MainWindow.xaml`) and a `ContentControl` bound to `CurrentStep`. The existing `Step1–4` `DataTemplate` registrations move from `MainWindow.xaml` into `UPNToolView.xaml`'s resources.

### AttributeToolViewModel

Owns `List<AttributeChangeEntry>`, the AD service reference, a `CurrentStep` property, and a `GoTo(int)` / `Reset()` method — same pattern as `UPNToolViewModel`.

`AttrToolView.xaml` is the corresponding view: step indicator bar + `ContentControl` bound to `CurrentStep`, with `DataTemplate` registrations for the four attribute-editor step viewmodels.

`MainWindow.xaml` registers only three top-level DataTemplates: `HomeViewModel → HomeView`, `UPNToolViewModel → UPNToolView`, `AttributeToolViewModel → AttrToolView`.

---

## Home Screen

`HomeView` / `HomeViewModel` — a launcher with two cards.

```
┌──────────────────────────────────────────────────────────────┐
│  AD Tool                                                      │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────┐  ┌────────────────────────┐  │
│  │  UPN Bulk Modifier         │  │  Attribute Editor      │  │
│  │                            │  │                        │  │
│  │  Change user UPNs and      │  │  Bulk-set Department,  │  │
│  │  proxy addresses in bulk   │  │  custom attributes,    │  │
│  │                            │  │  and other AD fields   │  │
│  │         [Launch]           │  │        [Launch]        │  │
│  └────────────────────────────┘  └────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

`HomeViewModel` has no state — just two `RelayCommand` properties that delegate to `AppShellViewModel`.

---

## Attribute Editor

### Data Model

```csharp
public class AttributeChangeEntry : BaseViewModel
{
    public string UserUPN { get; set; }
    public string? DisplayName { get; set; }
    public ValidationStatus ValidationStatus { get; set; }
    public ExecutionStatus ExecutionStatus { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorDetail { get; set; }

    // LDAP attribute name → new value (null/empty = skip at execute time)
    public Dictionary<string, string?> Attributes { get; set; } = new();
}
```

### Step 1 — Input

The grid uses a `DataTable` as `ItemsSource` (not `ObservableCollection`) so columns can be added and removed at runtime without code-behind column management. `AttrStep1InputViewModel` converts the `DataTable` to `List<AttributeChangeEntry>` when the user clicks Next.

**CSV import.** Column headers are matched case-insensitively against a built-in alias map. The identity column (`UPN` / `UserPrincipalName`) is required; all other columns are optional. Any unrecognised header is used as a raw LDAP attribute name (advanced mode).

| CSV header | LDAP attribute |
|---|---|
| `UPN`, `UserPrincipalName` | identity (not written to AD) |
| `Department` | `department` |
| `Description` | `description` |
| `Title` | `title` |
| `Company` | `company` |
| `Office` | `physicalDeliveryOfficeName` |
| `Phone` | `telephoneNumber` |
| `Manager` | `manager` |
| `CustomAttribute1`–`CustomAttribute15` | `extensionAttribute1`–`extensionAttribute15` |
| Any other value | used verbatim as the LDAP attribute name |

**Manual entry.**
- **Add Column** opens a picker: a scrollable list of all well-known attributes (checked/unchecked) plus a text field for a raw LDAP attribute name. Clicking OK adds the selected columns to the `DataTable`.
- **Browse AD** reuses the existing `AdBrowserDialog` to add users (same as the UPN tool). Added users appear as new rows with the identity column pre-filled.
- **Add Row** inserts a blank row for manual UPN entry.

### Step 2 — Validate

For each entry, looks up the user by UPN in AD and confirms they exist. No conflict detection is needed (no uniqueness constraint on general attributes).

- Pre-pass: any two rows with the same `UserUPN` (case-insensitive) are immediately flagged `DuplicateUPN` without hitting AD (same pattern as the UPN tool's same-batch duplicate check).
- AD queries run in parallel via `Task.WhenAll` for the remaining entries.
- Error types: `UserNotFound`, `UnexpectedError`.
- Same Remove Invalid Rows / Back UI as the UPN tool.

### Step 3 — Preview

Grid columns: **Display Name**, **UPN**, then one column per attribute in the run. Each cell shows the value that will be written. Blank cells are shown as empty and will be skipped at execute time.

### Step 4 — Execute

For each entry the tool:
1. Fetches the `DirectoryEntry` by UPN
2. For each non-blank attribute in `entry.Attributes`, sets `de.Properties[ldapName].Value = value`
3. Calls `de.CommitChanges()` once per user

Blank/null values in `entry.Attributes` are skipped — they do not clear the attribute. Same success/failure row display as the UPN tool, with expandable error detail. **Export CSV** columns: `UPN`, `DisplayName`, `Status`, `ErrorTitle`, `ErrorDetail`.

---

## IAdService Changes

One new method:

```csharp
Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes);
```

Returns the existing `ExecutionResult` type. `AdServiceStub` returns `ExecutionResult(true)`. `AdService` implements the `DirectoryEntry` write described above.

---

## Files

| File | Change |
|------|--------|
| `ADTool/ViewModels/MainViewModel.cs` | Rename/replace with `AppShellViewModel` |
| `ADTool/Views/MainWindow.xaml` | Remove step indicator bar; bind to `AppShellViewModel.CurrentView`; update title binding |
| `ADTool/ViewModels/HomeViewModel.cs` | New — two launch commands |
| `ADTool/Views/HomeView.xaml` | New — two-card launcher |
| `ADTool/ViewModels/UPNToolViewModel.cs` | New — thin wrapper around existing 4 step VMs; "Start New Run" calls `ReturnHome` |
| `ADTool/Views/UPNToolView.xaml` | New — step indicator + ContentControl for CurrentStep; holds existing Step1–4 DataTemplates |
| `ADTool/ViewModels/AttributeToolViewModel.cs` | New — owns attribute editor 4 steps |
| `ADTool/Views/AttrToolView.xaml` | New — step indicator + ContentControl for attribute editor steps |
| `ADTool/Models/AttributeChangeEntry.cs` | New |
| `ADTool/Models/AttributeColumnMap.cs` | New — static alias map + well-known attribute list |
| `ADTool/Services/IAdService.cs` | Add `UpdateAttributesAsync` |
| `ADTool/Services/AdService.cs` | Implement `UpdateAttributesAsync` |
| `ADTool/Services/AdServiceStub.cs` | Stub `UpdateAttributesAsync` |
| `ADTool/ViewModels/AttrStep1InputViewModel.cs` | New |
| `ADTool/Views/AttrStep1InputView.xaml` | New |
| `ADTool/ViewModels/AttrStep2ValidateViewModel.cs` | New |
| `ADTool/Views/AttrStep2ValidateView.xaml` | New |
| `ADTool/ViewModels/AttrStep3PreviewViewModel.cs` | New |
| `ADTool/Views/AttrStep3PreviewView.xaml` | New |
| `ADTool/ViewModels/AttrStep4ExecuteViewModel.cs` | New |
| `ADTool/Views/AttrStep4ExecuteView.xaml` | New |
| `ADTool/App.xaml.cs` | Wire `AppShellViewModel` as window DataContext |

---

## Testing

- `AppShellViewModelTests` — launching tools sets `CurrentView`; returning home resets it
- `HomeViewModelTests` — commands delegate to shell
- `UPNToolViewModelTests` — Start New Run returns to home (not Step 1)
- `AttributeColumnMapTests` — alias resolution for all known headers; unknown header passes through verbatim
- `AttrStep1InputViewModelTests` — CSV import maps headers correctly; Add Column adds DataTable column; Browse AD adds rows
- `AttrStep2ValidateViewModelTests` — same-batch duplicate UPN pre-pass; UserNotFound sets status; valid user sets Valid
- `AttrStep3PreviewViewModelTests` — entries flow through unchanged
- `AttrStep4ExecuteViewModelTests` — blank attributes skipped; non-blank attributes written; failure sets error fields

No new AD integration tests — `UpdateAttributesAsync` requires a live connection.

---

## Out of Scope

- Clearing (nulling) an attribute — blank means skip, not clear
- Reading current attribute values to show a before/after diff in Preview
- Multi-value attributes (e.g. `proxyAddresses`) — single-value writes only
- Creating or deleting AD users
- A third tool function
