# AD UPN Bulk Modifier

A WPF .NET 8 desktop tool for bulk-modifying Active Directory user UPNs (User Principal Names). Changes are previewed before execution, and proxy addresses are automatically updated alongside the UPN.

## Requirements

- Windows 10 / 11
- Domain-joined machine (or connectivity to a domain controller)
- An account that is a member of the **Domain Admins** group (required at startup)

## Installation

Download `ADTool-vX.X.X-win-x64.zip` from the [latest release](https://github.com/TheM0NKEY/AD-Tool/releases/latest), extract all files into the same folder, and run `ADTool.exe`. Windows will prompt for UAC elevation. No installer or .NET runtime required.

## Usage

### Running the tool

Double-click `ADTool.exe`, or launch from the command line:

```
ADTool.exe
```

The tool opens to a home screen where you choose between the **UPN Bulk Modifier** and the **Attribute Editor**. It checks at startup whether the current user is a member of the **Domain Admins** group. If not, access is denied and the tool exits.

To run in **dry-run mode** (no changes written to AD):

```
ADTool.exe --dry-run
```

Dry-run mode validates entries and simulates execution without touching Active Directory. The Domain Admin check is skipped in this mode. Useful for testing or previewing changes before committing.

---

### Step 1 — Input

Add the UPN changes you want to make. There are four ways to populate the list:

**Browse AD**
Click **Browse AD…** to open a dialog that lets you explore the Active Directory OU tree. Select an OU to see all users under it (searched recursively). Check the users you want and click **Add Selected to List** to add them directly to the grid with `NewUPN` left blank, ready for the bulk suffix-swap tool. Alternatively, click **Export to CSV** to save the selected users to a file for external editing.

**Import from CSV**
Click **Import CSV** and select a file. The CSV must have an `OldUPN` column header (case-insensitive). A `NewUPN` column is optional — if omitted, all imported rows will have `NewUPN` left blank so you can fill them in using the bulk suffix-swap tool. Rows with duplicate `OldUPN` values are skipped with a warning.

Full two-column example:
```csv
OldUPN,NewUPN
alice@old.contoso.com,alice@contoso.com
bob@old.contoso.com,bob@contoso.com
```

OldUPN-only example (NewUPN filled in via bulk suffix swap):
```csv
OldUPN
alice@old.contoso.com
bob@old.contoso.com
```

**Bulk suffix swap**
Enter the old suffix and new suffix in the toolbar fields, then click **Apply**. All rows whose `OldUPN` ends with the old suffix will have their `NewUPN` updated automatically.

**Manual entry**
Click **Add Row** to insert a blank row and type directly into the grid.

Click **Next** when the list is ready. The grid must have at least one entry.

---

### Step 2 — Validate

The tool queries Active Directory for each entry and checks:

- The `OldUPN` exists as a user in the domain
- The `NewUPN` is not already assigned to another user
- The suffix of the `NewUPN` is a registered UPN suffix in the forest

Each row shows a status icon (✔ valid / ✘ error) and an error description for any failures.

If there are invalid rows, you can:
- Click **Remove invalid rows** to drop them and continue with the valid ones
- Click **Back** to return to Step 1 and fix the data

Click **Next** once all remaining rows are valid.

---

### Step 3 — Preview

Review the full list of changes before anything is written. The grid shows:

| Column | Description |
|--------|-------------|
| Display Name | The user's current display name from AD |
| Old UPN | The current UPN that will be replaced |
| New UPN | The new UPN that will be set |
| Proxy address added | `smtp:OldUPN` — the old address demoted to secondary |
| New primary SMTP | `SMTP:NewUPN` — the new address promoted to primary |
| mail | Set to `NewUPN` — keeps the primary email attribute in sync for M365 and on-premises applications |
| mailNickname | Set to the prefix of `NewUPN` (before `@`) — the Exchange alias synced to Entra ID by Entra Connect |

Click **Back** to return to Step 2, or **Execute Changes** to proceed.

---

### Step 4 — Execute

Changes are applied sequentially. For each user, the tool:

1. Sets `userPrincipalName` to the new UPN
2. Demotes the old primary SMTP proxy address (`SMTP:oldUPN` → `smtp:oldUPN`)
3. Adds the new UPN as the primary SMTP proxy address (`SMTP:newUPN`)
4. Sets `mail` to the new UPN
5. Sets `mailNickname` to the prefix of the new UPN (the part before `@`)

Each row shows a success (✔) or failure (✘) indicator. Failures expand to show a title and detail message. If technical details are available (e.g. an exception message from AD), a nested **Technical details** expander provides the raw error.

**Export CSV** saves a results file with columns: `OldUPN`, `NewUPN`, `DisplayName`, `Status`, `ErrorTitle`, `ErrorDetail`.

**Start New Run** clears all data and returns to Step 1.

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

**Step 4 — Execute.** Writes all non-blank attribute values via `DirectoryEntry.CommitChanges()`. **Export CSV** columns: `UPN`, `DisplayName`, `Status`, `ErrorTitle`, `ErrorDetail`. **Start New Run** returns to the home screen.

---

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
git clone https://github.com/TheM0NKEY/AD-Tool.git
cd AD-Tool
dotnet build ADTool/ADTool.csproj
```

To publish a self-contained executable:

```
dotnet publish ADTool/ADTool.csproj -c Release -r win-x64
```

Output: `ADTool/bin/Release/net8.0-windows/win-x64/publish/ADTool.exe`

To run the test suite:

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```
