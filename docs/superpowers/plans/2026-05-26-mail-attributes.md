# mail and mailNickname Attribute Updates — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When executing a UPN change, also update the `mail` and `mailNickname` AD attributes to match, and show both values as columns in the Step 3 preview grid.

**Architecture:** `UPNChangeEntry` gains a computed `MailNickname` property (derived from `NewUPN`). `AdService.UpdateUserAsync` writes `mail` and `mailNickname` to the `DirectoryEntry` in the same `CommitChanges()` call as `proxyAddresses`. `Step3PreviewView.xaml` gets two new read-only columns. README Step 4 table is updated.

**Tech Stack:** C# / WPF .NET 8, `System.DirectoryServices.DirectoryEntry`, xunit

---

## Files

| File | Change |
|------|--------|
| `ADTool/Models/UPNChangeEntry.cs` | Add `MailNickname` computed property; fire `PropertyChanged` for it when `NewUPN` changes |
| `ADTool/Views/Step3PreviewView.xaml` | Add `mail` and `mailNickname` columns to the preview DataGrid |
| `ADTool/Services/AdService.cs` | Set `mail` and `mailNickname` on the DirectoryEntry in `UpdateUserAsync` |
| `README.md` | Add `mail` and `mailNickname` rows to the Step 4 execute table |
| `ADTool.Tests/UPNChangeEntryTests.cs` | Add three tests for `MailNickname` |

---

## Task 1: MailNickname property on UPNChangeEntry

**Files:**
- Modify: `ADTool.Tests/UPNChangeEntryTests.cs`
- Modify: `ADTool/Models/UPNChangeEntry.cs`

### Context

`UPNChangeEntry` is in `ADTool/Models/UPNChangeEntry.cs`. It already has `NewUPN` (string property with `PropertyChanged`). The test file is `ADTool.Tests/UPNChangeEntryTests.cs`. Tests use xunit; `using Xunit;` is already present in that file (see existing tests). `GlobalUsings.cs` already imports `ADTool.Models`.

The property to add:
```csharp
public string MailNickname => NewUPN.Contains('@') ? NewUPN.Split('@')[0] : NewUPN;
```

And `NewUPN` setter must also notify for `MailNickname` so the WPF grid refreshes when `NewUPN` changes.

---

- [ ] **Step 1: Write three failing tests**

Append to `ADTool.Tests/UPNChangeEntryTests.cs` (inside the existing `UPNChangeEntryTests` class, before the closing `}`):

```csharp
[Fact]
public void MailNickname_ReturnsUpnPrefix()
{
    var entry = new UPNChangeEntry { NewUPN = "alice@contoso.com" };
    Assert.Equal("alice", entry.MailNickname);
}

[Fact]
public void MailNickname_NoAtSign_ReturnsFullValue()
{
    var entry = new UPNChangeEntry { NewUPN = "alice" };
    Assert.Equal("alice", entry.MailNickname);
}

[Fact]
public void SettingNewUpn_FiresPropertyChangedForMailNickname()
{
    var entry = new UPNChangeEntry();
    var fired = new List<string?>();
    entry.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

    entry.NewUPN = "bob@contoso.com";

    Assert.Contains(nameof(UPNChangeEntry.MailNickname), fired);
}
```

- [ ] **Step 2: Run to confirm they fail**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "MailNickname|FiresPropertyChangedForMailNickname"
```

Expected: 3 failures — `MailNickname` does not exist yet.

- [ ] **Step 3: Implement `MailNickname` in UPNChangeEntry**

In `ADTool/Models/UPNChangeEntry.cs`, update the `NewUPN` setter and add the computed property. The full updated block (replace the existing `NewUPN` property and add `MailNickname` after it):

```csharp
public string NewUPN
{
    get => _newUpn;
    set
    {
        _newUpn = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(MailNickname));
    }
}

public string MailNickname => NewUPN.Contains('@') ? NewUPN.Split('@')[0] : NewUPN;
```

- [ ] **Step 4: Run tests to confirm they pass**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj --filter "MailNickname|FiresPropertyChangedForMailNickname"
```

Expected: 3 passing.

- [ ] **Step 5: Run full suite to check no regressions**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass (was 71 before this task).

- [ ] **Step 6: Commit**

```
git add ADTool/Models/UPNChangeEntry.cs ADTool.Tests/UPNChangeEntryTests.cs
git commit -m "feat: add MailNickname computed property to UPNChangeEntry"
```

---

## Task 2: Add mail and mailNickname columns to Step 3 preview

**Files:**
- Modify: `ADTool/Views/Step3PreviewView.xaml`

### Context

The DataGrid in `ADTool/Views/Step3PreviewView.xaml` currently has 5 columns: Display Name, Old UPN, New UPN, Proxy Address Added, New Primary SMTP. Add two more after "New Primary SMTP":

- **mail** — binds to `NewUPN` (they are always identical; showing it makes the change explicit to the user)
- **mailNickname** — binds to `MailNickname` (the computed property added in Task 1)

No code-behind changes needed.

---

- [ ] **Step 1: Add the two columns**

In `ADTool/Views/Step3PreviewView.xaml`, locate the closing `</DataGrid.Columns>` tag (currently after the "New Primary SMTP" `DataGridTemplateColumn`). Insert these two columns immediately before `</DataGrid.Columns>`:

```xml
<DataGridTextColumn Header="mail"         Binding="{Binding NewUPN}"       Width="*"/>
<DataGridTextColumn Header="mailNickname" Binding="{Binding MailNickname}"  Width="150"/>
```

- [ ] **Step 2: Build to confirm no XAML errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add ADTool/Views/Step3PreviewView.xaml
git commit -m "feat: add mail and mailNickname columns to Step 3 preview grid"
```

---

## Task 3: Write mail and mailNickname to AD in UpdateUserAsync

**Files:**
- Modify: `ADTool/Services/AdService.cs`

### Context

`UpdateUserAsync` in `ADTool/Services/AdService.cs` (around line 38) currently:
1. Sets `user.UserPrincipalName = newUpn` and calls `user.Save()`
2. Gets the `DirectoryEntry` via `user.GetUnderlyingObject()`
3. Updates `proxyAddresses` with `proxies.Clear()` / `proxies.Add()` loop
4. Calls `de.CommitChanges()`

Add `mail` and `mailNickname` writes between step 3 and step 4 — they are committed in the same `CommitChanges()` call, so no extra AD round-trip.

There are no unit tests for `UpdateUserAsync` itself because it requires a live AD connection. The logic here (string split on `@`) is the same as `MailNickname` in `UPNChangeEntry`, which is already tested.

---

- [ ] **Step 1: Add the two property writes**

In `ADTool/Services/AdService.cs`, locate the `UpdateUserAsync` method. Find this block:

```csharp
proxies.Clear();
foreach (var addr in updated)
    proxies.Add(addr);

de.CommitChanges();
```

Replace it with:

```csharp
proxies.Clear();
foreach (var addr in updated)
    proxies.Add(addr);

de.Properties["mail"].Value = newUpn;
de.Properties["mailNickname"].Value = newUpn.Contains('@') ? newUpn.Split('@')[0] : newUpn;

de.CommitChanges();
```

- [ ] **Step 2: Build to confirm no errors**

```
dotnet build ADTool/ADTool.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run full test suite**

```
dotnet test ADTool.Tests/ADTool.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```
git add ADTool/Services/AdService.cs
git commit -m "feat: update mail and mailNickname attributes on UPN change"
```

---

## Task 4: Update README

**Files:**
- Modify: `README.md`

### Context

The README Step 4 section has two parts to update:

**Part A** — the numbered list "For each user, the tool:" currently has 3 items:
1. Sets `userPrincipalName` to the new UPN
2. Demotes the old primary SMTP proxy address
3. Adds the new UPN as the primary SMTP proxy address

Add two more:

4. Sets `mail` to the new UPN
5. Sets `mailNickname` to the prefix of the new UPN (the part before `@`)

**Part B** — the grid table after the list currently has 5 rows (Display Name, Old UPN, New UPN, Proxy address added, New primary SMTP). Add two more rows.

---

- [ ] **Step 1: Update the numbered list**

In `README.md`, find:

```
1. Sets `userPrincipalName` to the new UPN
2. Demotes the old primary SMTP proxy address (`SMTP:oldUPN` → `smtp:oldUPN`)
3. Adds the new UPN as the primary SMTP proxy address (`SMTP:newUPN`)
```

Replace with:

```
1. Sets `userPrincipalName` to the new UPN
2. Demotes the old primary SMTP proxy address (`SMTP:oldUPN` → `smtp:oldUPN`)
3. Adds the new UPN as the primary SMTP proxy address (`SMTP:newUPN`)
4. Sets `mail` to the new UPN
5. Sets `mailNickname` to the prefix of the new UPN (the part before `@`)
```

- [ ] **Step 2: Update the preview table**

Find the table in the Step 4 section:

```
| New primary SMTP | `SMTP:NewUPN` — the new address promoted to primary |
```

After that line, add:

```
| mail | Set to `NewUPN` — keeps the primary email attribute in sync for M365 and on-premises applications |
| mailNickname | Set to the prefix of `NewUPN` (before `@`) — the Exchange alias synced to Entra ID by Entra Connect |
```

- [ ] **Step 3: Commit**

```
git add README.md
git commit -m "docs: document mail and mailNickname updates in README"
```

---

## Task 5: Push

- [ ] **Step 1: Push all commits**

```
git push
```
