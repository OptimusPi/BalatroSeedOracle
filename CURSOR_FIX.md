# Cursor IDE Crash Fix Guide

## Problem
Cursor opens and immediately closes when opening `X:\BalatroSeedOracle\` without any error message.

## Quick Fixes (Try These First)

### 1. Clear Cursor Workspace State
```powershell
# Close Cursor completely first, then run:
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\User\workspaceStorage\*"
```

### 2. Open via Workspace File Instead
Instead of:
```powershell
cursor .\BalatroSeedOracle\
```

Try:
```powershell
cursor .\BalatroSeedOracle.code-workspace
```

### 3. Clear Cursor Cache
```powershell
# Close Cursor first, then:
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\Cache\*"
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\CachedData\*"
```

### 4. Check for Corrupted Extensions
```powershell
# Disable all extensions temporarily:
# In Cursor: Ctrl+Shift+X → Disable All Extensions
# Then try opening the folder again
```

### 5. Try Opening a Subfolder First
```powershell
# Try opening a smaller subfolder:
cd X:\BalatroSeedOracle\external\Motely
cursor .
```

### 6. Check File Watcher Limits
The workspace excludes many files, but if you have thousands of files, Windows file watcher might be overwhelmed.

**Check file count:**
```powershell
cd X:\BalatroSeedOracle
(Get-ChildItem -Recurse -File | Measure-Object).Count
```

**If > 100,000 files, try:**
- Increase Windows file watcher limit (requires admin)
- Or temporarily exclude more folders in `.cursorignore`

### 7. Check Cursor Logs
```powershell
# View recent logs:
Get-Content "$env:APPDATA\Cursor\logs\*.log" -Tail 50 | Select-String -Pattern "error|crash|exception" -Context 2
```

### 8. Run Cursor with Verbose Logging
```powershell
cursor .\BalatroSeedOracle\ --verbose --log trace
```

### 9. Check for Symlinks/Junctions
```powershell
# Check if external folder is a symlink:
cd X:\BalatroSeedOracle
Get-Item external | Select-Object LinkType, Target
```

If `external` is a symlink/junction, Cursor might have issues. Try:
- Opening the actual target folder instead
- Or removing the symlink and using a real folder

### 10. Reset Cursor Settings
```powershell
# Backup first!
Copy-Item "$env:APPDATA\Cursor\User\settings.json" "$env:APPDATA\Cursor\User\settings.json.backup"

# Then reset:
Remove-Item "$env:APPDATA\Cursor\User\settings.json"
```

## Most Likely Causes

1. **Corrupted workspace state** - Fix: Clear workspace storage (#1)
2. **File watcher overload** - Fix: Check file count (#6)
3. **Symlink issues** - Fix: Check symlinks (#9)
4. **Extension crash** - Fix: Disable extensions (#4)
5. **Memory issues** - Fix: Check workspace file memory settings (already set to 4096MB)

## Still Not Working?

1. **Check Windows Event Viewer:**
   - Win+R → `eventvwr.msc`
   - Windows Logs → Application
   - Look for Cursor crashes

2. **Try a fresh Cursor install:**
   - Uninstall Cursor
   - Delete `%APPDATA%\Cursor` folder
   - Reinstall Cursor
   - Try opening folder again

3. **Report to Cursor support:**
   - Include logs from `%APPDATA%\Cursor\logs\`
   - Include workspace file
   - Describe what happens

## Prevention

- Keep workspace file updated
- Don't exclude too many folders (can confuse file watcher)
- Regularly clear workspace storage
- Monitor file count in workspace



