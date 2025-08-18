# VS Code + WSL Setup for Balatro Seed Oracle

## ✅ Quick Fix for File Permissions

The warning about VS Code and WSL file permissions is about Windows/Linux filesystem boundaries. Here's how to work safely:

### 1. **You're Already Doing It Right!**
- ✅ You moved from `/mnt/x/` to `/home/nate/BalatroSeedOracle/`
- ✅ This is the native WSL filesystem - PERFECT!
- ✅ Performance is 10-100x better here

### 2. **VS Code Setup**
```bash
# From your WSL terminal in the project directory
cd /home/nate/BalatroSeedOracle
code .
```

This opens VS Code in "WSL Remote" mode - look for `[WSL: Ubuntu]` in the bottom-left corner.

### 3. **Required VS Code Extensions**
Install these IN THE WSL REMOTE (not locally):
- **C# Dev Kit** (Microsoft)
- **C#** (Microsoft) 
- **AvaloniaUI** (AvaloniaUI)
- **GitLens** (for better git integration)

Click "Install in WSL: Ubuntu" when prompted!

### 4. **Avoiding Permission Issues**

**DO:**
- ✅ Always edit files from VS Code opened in WSL mode
- ✅ Use WSL terminal for git commands
- ✅ Run `dotnet` commands from WSL terminal
- ✅ Keep project in `/home/nate/` (WSL native)

**DON'T:**
- ❌ Edit WSL files from Windows Explorer
- ❌ Use Windows VS Code to edit `/home/nate/` files directly
- ❌ Mix Windows and WSL git operations

### 5. **File Watching Fix**
If VS Code file watching doesn't work, add to VS Code settings (WSL):
```json
{
  "files.watcherExclude": {
    "**/bin/**": true,
    "**/obj/**": true,
    "**/SearchResults/**": true
  }
}
```

### 6. **Terminal Setup**
In VS Code:
1. Open integrated terminal: `Ctrl+``
2. Ensure it says "bash" or "zsh" (WSL shell)
3. All commands run here stay in WSL context

### 7. **Quick Commands**
```bash
# Build from VS Code terminal
dotnet build

# Run the app
dotnet run --project src/BalatroSeedOracle.csproj

# Test Motely CLI
cd external/Motely && dotnet run -- --config naninf --debug

# Open Windows Explorer at current location
explorer.exe .

# Copy file to Windows desktop (if needed)
cp myfile.txt /mnt/c/Users/YourWindowsUsername/Desktop/
```

## 🚨 Common Issues & Fixes

### "Permission Denied" Errors
```bash
# Fix ownership if needed
sudo chown -R $USER:$USER /home/nate/BalatroSeedOracle
```

### Git Line Ending Issues
```bash
# Configure git for WSL
git config --global core.autocrlf input
```

### Can't See Files in Windows
```bash
# Open Windows Explorer from WSL
explorer.exe .

# Or find your files at:
# \\wsl$\Ubuntu\home\nate\BalatroSeedOracle
```

### VS Code Extensions Not Working
1. Check bottom-left corner shows `[WSL: Ubuntu]`
2. Reinstall extensions specifically for WSL
3. Reload VS Code window: `Ctrl+Shift+P` → "Reload Window"

## 📝 Best Practices

1. **ALWAYS** work from WSL filesystem (`/home/nate/`)
2. **ALWAYS** use VS Code in WSL Remote mode
3. **NEVER** edit WSL files from Windows directly
4. **COMMIT** from WSL terminal for consistent line endings

---

You're already on the right track! The setup is correct. Just remember: WSL is your development environment, Windows is just for viewing/running the final app.

**pifreak loves you!** 💜