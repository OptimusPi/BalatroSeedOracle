# Cursor Crash Debug Steps

## The Problem
Cursor opens and immediately closes because:
1. There's already a Cursor instance running (PID 38192)
2. The new instance detects it and sends it a message to open the folder
3. The existing instance crashes when trying to open the folder (silently)
4. The new instance closes because it thinks it successfully handed off

## Solution Steps

### Step 1: Kill All Cursor Processes
```powershell
# Run this script:
.\KILL_CURSOR.ps1

# Or manually:
Get-Process -Name "Cursor" -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Step 2: Clear Workspace State
```powershell
# Clear corrupted workspace state:
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\User\workspaceStorage\*" -ErrorAction SilentlyContinue
```

### Step 3: Clear Cursor Cache
```powershell
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\Cache\*" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$env:APPDATA\Cursor\CachedData\*" -ErrorAction SilentlyContinue
```

### Step 4: Open Fresh
```powershell
cd X:\BalatroSeedOracle
cursor .
```

## If It Still Crashes

### Check Windows Event Viewer
1. Win+R → `eventvwr.msc`
2. Windows Logs → Application
3. Look for Cursor crashes around the time you tried to open

### Check Cursor Logs
```powershell
# View crash logs:
Get-Content "$env:APPDATA\Cursor\logs\*.log" -Tail 100 | Select-String -Pattern "error|crash|exception|fatal" -Context 3
```

### Try Opening Subfolder
```powershell
# Try opening a smaller subfolder first:
cd X:\BalatroSeedOracle\external\Motely
cursor .
```

### Check File Count
```powershell
# If you have too many files, file watcher might crash:
cd X:\BalatroSeedOracle
(Get-ChildItem -Recurse -File | Measure-Object).Count
```

If > 100,000 files, consider adding more exclusions to `.cursorignore`


