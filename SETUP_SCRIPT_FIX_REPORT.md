# Setup Script Fix Report

## Root Cause

The original `setup-dev.ps1` used **string matching** to replace placeholder values:

```powershell
# BROKEN: string pattern matching
'ServerCertPassword value="CHANGE_ME"' = "ServerCertPassword value=""$password"""
```

This failed because:

1. **XML format mismatch**: The actual `App.config` XML attribute order can vary. The text `ServerCertPassword value="CHANGE_ME"` does not match `key="ServerCertPassword" value="CHANGE_ME"` (missing `key=` prefix).
2. **Whitespace sensitivity**: Even minor formatting differences break string pattern matching.
3. **Encoding issues**: When `Get-Content -Raw` loads the file, escape characters may differ.
4. **No validation feedback**: The script reported success but the file went unchanged.

## Fix Applied

Replaced string manipulation with **proper XML DOM parsing**:

### Before (broken):
```powershell
# String replacement - fragile, fails on XML format
$serverConfigContent = Get-Content $serverConfig -Raw
$serverConfigContent = $serverConfigContent -replace [regex]::Escape($old), $replacements[$old]
Set-Content -Path $serverConfig -Value $serverConfigContent
```

### After (fixed):
```powershell
# XML DOM parsing - robust, matches on key attribute
[xml]$config = Get-Content $serverConfigPath -Raw
$setting = $config.configuration.appSettings.SelectSingleNode("add[@key='ServerCertPassword']")
$setting.value = $newPassword
$config.Save($serverConfigPath)
```

### Key changes:
1. `Get-Content -Raw` → cast to `[xml]` object
2. `SelectSingleNode("add[@key='X']")` → XPath-based lookup
3. `$node.value = ...` → direct DOM attribute assignment
4. `$config.Save(path)` → proper XML serialization

### Validation after save:
Instead of just checking files exist, the script now:
1. **Reloads** the saved XML from disk
2. **Reads back** each value using `SelectSingleNode`
3. **Verifies** value != `CHANGE_ME` (or placeholder patterns)
4. **Prints** explicit SUCCESS messages per key

## Validation After Fix

The validation now explicitly confirms:

```
SUCCESS:
  ServerCertPassword updated
  DbPassword updated
  AesKey updated
  AesIV updated
  ClientCertPassword updated
  All placeholders replaced
```

## Error Handling Added

Wrapped entire script in `try/catch`:

```powershell
try {
    # ... all setup logic ...
}
catch {
    Write-Host "SETUP FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to exit"
}
```

This ensures:
- Any error (cert generation failure, file access denied, XML parse error) is caught
- Error message is displayed
- Script waits for user acknowledgment before closing

## Files Modified

| File | Change |
|---|---|
| `scripts/setup-dev.ps1` | Sections 4-6 rewritten: replaced string replacement with XML DOM parsing + XPath. Added try/catch error handling. Enhanced validation with readback verification. |