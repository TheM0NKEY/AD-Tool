# Design: mail and mailNickname Attribute Updates

**Date:** 2026-05-26
**Status:** Approved

---

## Overview

When a UPN change is executed, two additional AD attributes must be updated to keep the user's identity consistent for on-premises applications and Microsoft 365 sync via Entra Connect:

- **`mail`** — single-value string; the user's primary email address. Synced directly to Azure AD `mail`. Must match the new primary SMTP proxy address.
- **`mailNickname`** (`msExchMailNickname`) — the Exchange alias prefix (the part before `@`). Synced to Azure AD `mailNickname`. Derived from the new UPN prefix.

Both attributes are written in the same `CommitChanges()` call as the existing `proxyAddresses` update — no extra AD round-trip.

---

## Changes

### `AdService.cs` — `UpdateUserAsync`

After the existing `proxyAddresses` update, before `de.CommitChanges()`:

```csharp
de.Properties["mail"].Value = newUpn;
de.Properties["mailNickname"].Value = newUpn.Contains('@') ? newUpn.Split('@')[0] : newUpn;
```

### `UPNChangeEntry.cs`

Add a computed read-only property:

```csharp
public string MailNickname => NewUPN.Contains('@') ? NewUPN.Split('@')[0] : NewUPN;
```

In the `NewUPN` setter, add notification so the grid updates live:

```csharp
OnPropertyChanged(nameof(MailNickname));
```

### `Step3PreviewView.xaml`

Two new columns appended to the existing DataGrid:

| Column | Binding | Width |
|--------|---------|-------|
| mail | `NewUPN` | `*` |
| mailNickname | `MailNickname` | `150` |

### `README.md`

Add `mail` and `mailNickname` rows to the Step 4 — Execute table.

---

## Testing

One new unit test on `UPNChangeEntry`: changing `NewUPN` updates `MailNickname` (verifies the `PropertyChanged` notification fires and the derived value is correct).

No new AD integration tests — `DirectoryEntry` property writes require a live AD connection.

---

## Out of Scope

- `sAMAccountName` — legacy logon name; changing it breaks domain logon and group policy
- Exchange-specific attributes (`msExchMailboxGuid`, `msExchHomeServerName`, `targetAddress`) — managed by Exchange Server / Entra Connect writeback in hybrid
- `displayName` — not email-related
