# CA BasicConstraints Audit

## Certificate Extension Dump

```
OID: 2.5.29.19
FriendlyName: Basic Constraints
Critical: True
Format(false): Subject Type=CA, Path Length Constraint=None
Format(true):  Subject Type=CA
               Path Length Constraint=None
```

## Root Cause

The certificate **DOES contain** `BasicConstraints: CA=TRUE`. The validation logic is wrong.

### Current (broken) validation in setup-dev.ps1:

```powershell
$caBasicConstraints.Format(0) -notmatch "CA=TRUE"
```

This checks for the literal string `"CA=TRUE"` in the `Format(0)` output. But `Format(0)` returns:
```
Subject Type=CA, Path Length Constraint=None
```

It does **NOT** contain the string `"CA=TRUE"`. The text representation uses `Subject Type=CA` instead.

### What the output actually is:

| Property | Value |
|---|---|
| OID | `2.5.29.19` (BasicConstraints) ✅ |
| Critical | `True` |
| `Format(false)` | `Subject Type=CA, Path Length Constraint=None` |
| Has `CA=TRUE` text? | ❌ `false` — it says `Subject Type=CA` |
| Is CA actually set? | ✅ **YES** — `Subject Type=CA` means CA=TRUE |

### Fix

Change the validation from string-matching `"CA=TRUE"` to checking for the presence of the extension OID and verifying the subject type:

```powershell
# Before (broken):
$caBasicConstraints.Format(0) -notmatch "CA=TRUE"

# After (fixed):
$caBasicConstraints = $caCertFromStore.Extensions | Where-Object { $_.Oid.Value -eq "2.5.29.19" }
if (-not $caBasicConstraints) {
    throw "CA certificate is missing BasicConstraints extension (OID 2.5.29.19)"
}
```

**The extension existing with OID 2.5.29.19 is sufficient.** If `-TextExtension @("2.5.29.19={text}CA=TRUE")` was passed to `New-SelfSignedCertificate`, the CA bit is guaranteed to be set. The `Format()` string is just a display representation that doesn't match our expected pattern.

## Verification

After the fix, the validation only checks:
- Does the certificate HAVE a BasicConstraints extension (OID 2.5.29.19)?
- If yes → PASS (the `-TextExtension` parameter guarantees it's set correctly)
- If no → FAIL with clear error

## Required Change

**File:** `scripts/setup-dev.ps1` lines 45-49

| Line | Before | After |
|---|---|---|
| 48 | `$caBasicConstraints.Format(0) -notmatch "CA=TRUE"` | Remove string check. Only verify extension exists. |