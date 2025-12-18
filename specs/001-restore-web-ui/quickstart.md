# Quickstart: Restore Missing Web UI

**Feature**: 001-restore-web-ui  
**Goal**: Restore functional Motely API test interface with SignalR WebSocket support

## Prerequisites

- Motely.API project with existing MotelyApiServer.cs
- Recovered files: app.js, indexOLD.html, MotelySearchDatabase.cs
- .NET 10.0 SDK
- Modern web browser

## Implementation Steps

### 1. Restore Web UI Files (30 minutes)

1. **Copy recovered files to wwwroot**:
   ```bash
   cd external/Motely/Motely.API/wwwroot
   cp indexOLD.html index.html
   cp ../../../recovered/app.js .
   cp ../../../recovered/app.css .  # if found
   ```

2. **Verify file structure**:
   ```
   Motely.API/wwwroot/
   ├── index.html     # Restored from indexOLD.html
   ├── app.js         # SignalR client code
   ├── app.css        # Responsive styling (if available)
   └── favicon.ico    # Icon file
   ```

### 2. Add SignalR Support (45 minutes)

1. **Install SignalR package**:
   ```bash
   cd external/Motely/Motely.API
   dotnet add package Microsoft.AspNetCore.SignalR
   ```

2. **Create SearchHub.cs** (already created):
   ```csharp
   // File: SearchHub.cs
   public class SearchHub : Hub
   {
       public async Task SubscribeToSearch(string searchId) { ... }
       public async Task UnsubscribeFromSearch(string searchId) { ... }
   }
   ```

3. **Add SignalR to existing server**:
   - Integrate SignalR alongside HttpListener
   - Broadcast search events to connected clients
   - Maintain existing HTTP API endpoints

### 3. Integration Testing (15 minutes)

1. **Start API server**:
   ```bash
   cd external/Motely
   dotnet run --project Motely.TUI
   ```

2. **Open browser** to `http://localhost:3141`
3. **Verify UI loads** with side-by-side layout
4. **Test SignalR connection** in browser dev tools
5. **Execute test search** and verify real-time updates

### 4. Validation Checklist

- [ ] Web UI loads with side-by-side layout
- [ ] JAML editor appears in left panel
- [ ] Results table appears in right panel
- [ ] Format button formats JAML syntax
- [ ] Start search button initiates searches
- [ ] Settings gear opens configuration modal
- [ ] Filter dropdown shows status indicators
- [ ] SignalR connection establishes successfully
- [ ] Real-time search progress updates work
- [ ] Responsive layout collapses on mobile
- [ ] API backend integration functions correctly

## Troubleshooting

### SignalR Connection Issues
- Check browser console for connection errors
- Verify `/signalr` endpoint is accessible
- Confirm SignalR hub is properly registered

### UI Layout Problems
- Verify CSS files are loading correctly
- Check responsive breakpoints in browser dev tools
- Confirm HTML structure matches expected layout

### API Integration Failures
- Test HTTP endpoints directly (POST /search)
- Verify JAML syntax is valid
- Check server logs for backend errors

## Success Criteria

✅ **Functional restoration**: All original UI features work correctly  
✅ **Real-time updates**: SignalR WebSocket connection provides live search progress  
✅ **Responsive design**: Layout adapts properly to different screen sizes  
✅ **API integration**: Backend communication functions as expected  

## Next Steps

After successful restoration:
1. Document any missing features compared to original
2. Consider incremental improvements (better error handling, enhanced UI)
3. Evaluate integration with broader BalatroSeedOracle ecosystem

## Reference Files

- **Spec**: [spec.md](spec.md) - Complete feature specification
- **Research**: [research.md](research.md) - Technical decisions and alternatives
- **Data Model**: [data-model.md](data-model.md) - Component structure and relationships
- **SignalR Contract**: [contracts/signalr-hub.md](contracts/signalr-hub.md) - WebSocket interface definition