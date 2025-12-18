# Data Model: Restore Missing Web UI

**Feature**: 001-restore-web-ui  
**Date**: 2025-12-16  
**Context**: Data structures for restored Motely API test UI

## Core Entities

### Web UI Components

**Purpose**: Client-side interface components that comprise the test UI

**Components**:
- `JAML Editor`: Left panel text editor with syntax highlighting and Format button
- `Results Table`: Right panel displaying search results in tabular format
- `Filter Dropdown`: Selection control showing saved filters with status indicators
- `Settings Gear`: Icon button for accessing configuration modal
- `Status Display`: Real-time feedback about search progress and connection state

**Relationships**:
- JAML Editor sends content to Search Session via API calls
- Results Table receives data from Search Session via SignalR updates
- Filter Dropdown loads from Saved Filters collection
- Settings Gear opens configuration modal with localStorage persistence

### SignalR Hub Interface

**Purpose**: WebSocket communication contract between client and server

**Hub Methods**:
- `SubscribeToSearch(searchId)`: Join real-time updates for specific search
- `UnsubscribeFromSearch(searchId)`: Stop receiving updates for search

**Client Events**:
- `SearchProgress`: Real-time progress updates during search execution
- `SeedFound`: Immediate notification when matching seed discovered
- `SearchCompleted`: Final event when search finishes successfully
- `SearchStopped`: Event when search is manually stopped
- `SearchError`: Error notification if search encounters problems

**Event Data Structures**:
- Progress events include: currentBatch, totalBatches, seedsSearched, searchSpeed
- Seed events include: seed value, score, timestamp
- Status events include: searchId, status, message

### Search Session State

**Purpose**: Represents active search operation with real-time updates

**Fields**:
- `SessionId`: Unique identifier for the search
- `FilterJaml`: JAML filter configuration being executed
- `Status`: Current state (Starting, Running, Paused, Completed, Failed)
- `StartTime`: When search was initiated
- `Progress`: Current completion percentage and metrics
- `Results`: Collection of matching seeds found
- `WebSocketClients`: List of connected clients receiving updates

**State Transitions**:
- Starting → Running → Completed
- Starting → Running → Paused → Running
- Any state → Failed (error condition)
- Running → Stopped (manual termination)

### Client Configuration

**Purpose**: User preferences and settings stored in browser localStorage

**Settings**:
- `batchSize`: Number of seeds to process per batch
- `threadCount`: Parallel processing threads for searches
- `wordlistSource`: Source of seed data (none, database, file)
- `databaseFile`: Selected database file for seed source
- `autoFormat`: Whether to auto-format JAML on typing
- `theme`: UI theme preference (light, dark, auto)

**Storage**:
- Persisted in browser localStorage as JSON
- Loaded on page initialization
- Updated through settings modal interface

### Saved Filters Collection

**Purpose**: Collection of user-created JAML filters with metadata

**Filter Properties**:
- `name`: User-assigned filter name
- `jamlContent`: JAML filter syntax
- `lastUsed`: Timestamp of last execution
- `searchStatus`: Current status (stopped, running, completed)
- `resultCount`: Number of results from last execution

**Status Indicators**:
- 🔴 Red indicator for stopped/inactive filters
- 🟢 Green indicator for currently running searches
- Visual feedback for user to understand filter states at a glance

## UI Layout Structure

### Desktop Layout (Side-by-side)

```text
+----------------------------------+----------------------------------+
|           JAML Editor            |          Results Table           |
|  [Format] [Settings⚙️] [Start]    |   | Seed      | Score | Status |  |
|                                  |   |-----------|-------|---------|  |
|  must:                           |   | ABC123    |  150  |   ✓     |  |
|    - joker: Perkeo               |   | DEF456    |  140  |   ✓     |  |
|      antes: [1, 2]               |   | GHI789    |  135  |   ✓     |  |
|  deck: Red                       |                                  |
|  stake: White                    |  [🔴🟢] Filter Status            |
|                                  |  [Search Progress: 45%]          |
+----------------------------------+----------------------------------+
```

### Mobile Layout (Vertical Stack)

```text
+------------------------------------------+
|              JAML Editor                 |
|  [Format] [Settings⚙️] [Start]            |
|                                          |
|  must:                                   |
|    - joker: Perkeo                       |
|      antes: [1, 2]                       |
|  deck: Red                               |
|  stake: White                            |
+------------------------------------------+
|             Results Table                |
|   | Seed      | Score | Status |         |
|   |-----------|-------|---------|         |
|   | ABC123    |  150  |   ✓     |         |
|   | DEF456    |  140  |   ✓     |         |
|                                          |
|  [🔴🟢] Filter Status                     |
|  [Search Progress: 45%]                  |
+------------------------------------------+
```

## API Integration Points

### HTTP REST Endpoints (Existing)

- `POST /search`: Initiate new search with JAML filter
- `GET /search?id={searchId}`: Get search status and results
- `POST /search/stop`: Stop running search
- `GET /filters`: Get saved filters list
- `DELETE /filters/{filterId}`: Delete saved filter

### SignalR WebSocket Events (Restore Target)

- **Client → Server**: `SubscribeToSearch(searchId)`
- **Server → Client**: `SearchProgress(progressData)`
- **Server → Client**: `SeedFound(seedData)`
- **Server → Client**: `SearchCompleted(completionData)`
- **Server → Client**: `SearchStopped(stopData)`
- **Server → Client**: `SearchError(errorData)`

### Data Flow

1. User creates/edits JAML in editor
2. Format button validates and prettifies JAML syntax
3. Start button sends POST /search to API
4. Client subscribes to SignalR updates for returned searchId
5. Real-time progress updates flow via WebSocket
6. Results populate in table as seeds are found
7. Status indicators update based on search state

## Browser Compatibility

### Required Features
- ES6 modules for SignalR client
- CSS Grid/Flexbox for responsive layout
- LocalStorage for settings persistence
- WebSocket support for real-time updates
- JSON parsing for API communication

### Target Browsers
- Chrome 80+ (desktop/mobile)
- Firefox 75+ (desktop/mobile)  
- Safari 13+ (desktop/mobile)
- Edge 80+ (desktop)

### Fallback Behavior
- If WebSocket fails, fall back to HTTP polling
- If localStorage unavailable, use session defaults
- If modern CSS unsupported, provide basic layout