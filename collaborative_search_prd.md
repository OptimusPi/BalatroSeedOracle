# Product Requirements Document: Collaborative Search Workspaces

## 🎯 Executive Summary

Enable multiple users to collaboratively search through Balatro seed space by distributing work batches across participants in real-time.

**Core Concept**: Split large search ranges into manageable batches, assign them to different users, and aggregate results in a shared workspace.

---

## 🚀 Problem Statement

- **Large search spaces** take too long for individual users (millions of 8-digit seeds)
- **Duplicate work** when multiple users search similar criteria independently  
- **No coordination** mechanism for community seed hunting projects
- **Inefficient resource usage** when users have idle compute time

---

## ✅ Success Criteria

- **10x faster** large-scale searches through distributed work
- **Zero duplicate work** through proper batch coordination
- **Simple setup** - create/join workspace in <30 seconds
- **Reliable coordination** - no lost batches or conflicts

---

## 🏗️ Technical Architecture (K.I.S.S.)

### Core Data Structures
```csharp
public class PublicLobby 
{
    public string Id { get; set; }                    // GUID
    public string Name { get; set; }                  // "Legendary Joker Hunt"
    public string CreatedBy { get; set; }             // "@Alice"
    public bool IsPublic { get; set; } = true;        // Always true for now
    public OuijaConfig SearchCriteria { get; set; }   // What we're searching for
    public uint StartSeed { get; set; }               // 10000000 
    public uint EndSeed { get; set; }                 // 99999999
    public int BatchSize { get; set; }                // 10000 seeds per batch
    public int CurrentBatch { get; set; }             // Next batch to assign
    public List<WorkerStatus> Workers { get; set; }   // Who's working
    public List<SearchResult> Results { get; set; }   // Found seeds
    public DateTime CreatedAt { get; set; }
    public LobbyStatus Status { get; set; }           // Active/Paused/Complete
}

public class WorkerStatus
{
    public string UserId { get; set; }                // "@Alice"
    public int AssignedBatch { get; set; }            // Batch number they're working on
    public DateTime LastHeartbeat { get; set; }       // When they last checked in
    public bool IsActive { get; set; }                // Currently searching
}

public class SearchBatch
{
    public int BatchNumber { get; set; }              // 0, 1, 2, 3...
    public uint StartSeed { get; set; }               // 10000000
    public uint EndSeed { get; set; }                 // 10009999
    public string AssignedTo { get; set; }            // "@Alice"
    public BatchStatus Status { get; set; }           // Assigned/InProgress/Complete
}

public enum LobbyStatus { Active, Paused, Complete }
```

### Simple Lobby Management
```csharp
public class LobbyManager
{
    // Get all public lobbies for browser
    public List<PublicLobby> GetPublicLobbies()
    {
        return _lobbies.Values
            .Where(l => l.IsPublic && l.Status == LobbyStatus.Active)
            .OrderByDescending(l => l.Workers.Count)
            .ToList();
    }
    
    // Anyone can join any public lobby
    public bool JoinLobby(string lobbyId, string userId)
    {
        var lobby = _lobbies[lobbyId];
        if (!lobby.IsPublic || lobby.Status != LobbyStatus.Active)
            return false;
            
        // Add worker if not already in lobby
        if (!lobby.Workers.Any(w => w.UserId == userId))
        {
            lobby.Workers.Add(new WorkerStatus 
            { 
                UserId = userId, 
                IsActive = true,
                LastHeartbeat = DateTime.UtcNow 
            });
        }
        return true;
    }
    
    // Get next available batch for a worker
    public SearchBatch? GetNextBatch(string lobbyId, string userId)
    {
        var lobby = _lobbies[lobbyId];
        var batch = lobby.CurrentBatch++;
        
        if (batch * lobby.BatchSize > (lobby.EndSeed - lobby.StartSeed))
            return null; // All done
            
        return new SearchBatch 
        {
            BatchNumber = batch,
            StartSeed = lobby.StartSeed + (uint)(batch * lobby.BatchSize),
            EndSeed = lobby.StartSeed + (uint)((batch + 1) * lobby.BatchSize - 1),
            AssignedTo = userId,
            Status = BatchStatus.Assigned
        };
    }
}
```

---

## 🎨 User Interface Design

### 1. Create Public Lobby
```
┌─────────────────────────────────────┐
│ 🆕 Create Public Search Lobby       │
├─────────────────────────────────────┤
│ Name: [Legendary Joker Hunt       ] │
│                                     │
│ 🔓 Public (anyone can join)         │
│                                     │
│ Search Range:                       │
│ From: [10000000] To: [99999999]     │
│                                     │
│ Batch Size: [10000] seeds           │
│ (10K recommended for most searches) │
│                                     │
│ 📋 Search Criteria:                 │
│ [Import from current filter]        │
│                                     │
│ Creator: [@YourGitHubHandle]        │
│                                     │
│ [🚀 Create & Start] [❌ Cancel]     │
└─────────────────────────────────────┘
```

### 2. Active Lobby Dashboard
```
┌─────────────────────────────────────────────────────────┐
│ 🔍 Legendary Joker Hunt (@Alice)           [⚙️Settings] │
├─────────────────────────────────────────────────────────┤
│ Progress: ████████░░ 82% (8,200/10,000 batches)        │
│ Found: 47 seeds | Active Workers: 3 | Public Lobby     │
│                                                         │
│ 👥 Workers:                                             │
│ • @Alice    [Batch 8,201] ████████░░ 80% [❤️ 2s ago]   │
│ • @Bob      [Batch 8,202] ██████░░░░ 60% [❤️ 5s ago]   │
│ • @Charlie  [Batch 8,203] ██░░░░░░░░ 20% [❤️ 1s ago]   │
│                                                         │
│ 🎯 Your Status: [🔄 Get Next Batch] [⏸️ Pause Work]     │
│                                                         │
│ 📊 Recent Results:                                      │
│ • Seed 87263541 - Legendary: Brainstorm + DNA (@Bob)   │
│ • Seed 84729163 - Legendary: Soul + Blueprint (@Alice) │
│ • Seed 82847392 - Legendary: Triboulet + Negative (@C) │
│                                                         │
│ [📋 View All Results] [📤 Export] [🔗 Share Lobby]     │
└─────────────────────────────────────────────────────────┘
```

### 3. Public Lobbies Browser
```
┌─────────────────────────────────────────────────────────┐
│ 🌐 Public Search Lobbies                    [🆕 Create] │
├─────────────────────────────────────────────────────────┤
│ 🔍 Filter: [Active] [All] [My Searches]                │
│                                                         │
│ 📋 Available Lobbies:                                   │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 🔥 Legendary Joker Hunt                   3 workers │ │
│ │ Range: 10M-99M | Progress: 82% | Est: 2h remaining │ │
│ │ by @Alice • 47 seeds found • [🚀 Join Lobby]       │ │
│ └─────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ⚫ Negative Tag Speedrun                  1 worker  │ │
│ │ Range: 50M-60M | Progress: 12% | Est: 4h remaining │ │
│ │ by @Bob • 2 seeds found • [🚀 Join Lobby]          │ │
│ └─────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 💎 Blueprint DNA Combo Hunt               5 workers │ │
│ │ Range: 20M-30M | Progress: 95% | Est: 30m remaining│ │
│ │ by @Charlie • 156 seeds found • [👀 View Results]  │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Your Name: [YourGitHubHandle] [⚙️ Settings]             │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Details

### Phase 1: Local Network (Simple)
- **SQLite database** shared on network drive
- **File locking** for batch assignment coordination
- **JSON files** for workspace configuration
- **Polling every 5 seconds** for status updates

### Phase 2: Cloud Sync (Future)
- **Firebase/Supabase** for real-time coordination
- **WebSocket** connections for live updates
- **Conflict resolution** for network issues

### Batch Assignment Algorithm
```csharp
// Simple round-robin with heartbeat checking
public SearchBatch GetNextBatch(string workspaceId, string userId)
{
    lock (_workspaceLock)
    {
        var workspace = _workspaces[workspaceId];
        
        // Clean up stale workers (no heartbeat >60 seconds)
        CleanupStaleWorkers(workspace);
        
        // Assign next sequential batch
        var batchNumber = workspace.CurrentBatch++;
        var startSeed = workspace.StartSeed + (batchNumber * workspace.BatchSize);
        var endSeed = Math.Min(startSeed + workspace.BatchSize - 1, workspace.EndSeed);
        
        if (startSeed > workspace.EndSeed)
            return null; // Search complete
            
        return new SearchBatch
        {
            BatchNumber = batchNumber,
            StartSeed = startSeed,
            EndSeed = endSeed,
            AssignedTo = userId
        };
    }
}
```

---

## 📋 User Stories

### Story 1: Create Public Lobby
**As a** search organizer
**I want to** create a public lobby for a specific search
**So that** anyone in the community can help find rare seeds faster

**Acceptance Criteria:**
- Can specify search range (start/end seeds)
- Can set batch size (recommended 10K)
- Can import current filter configuration
- Lobby is immediately visible in public browser
- Shows estimated completion time with projected workers

### Story 2: Browse and Join Lobbies
**As a** contributor
**I want to** browse public lobbies and join interesting searches
**So that** I can help with searches that match my interests/time

**Acceptance Criteria:**
- Can see all active public lobbies with progress/workers
- Can filter by lobby status (active/completing soon)
- One-click join - no invites or codes needed
- Can see who created the lobby and recent finds
- Shows estimated completion time for each lobby

### Story 3: Work in Lobby
**As a** lobby participant  
**I want to** contribute compute power and see collective progress
**So that** searches complete faster with community effort

**Acceptance Criteria:**
- Automatically gets assigned next available batch
- Shows progress on current batch
- Can pause/resume work at any time
- Submits results automatically when batch complete
- Can see other workers' progress and contributions

---

## 🚧 Technical Risks & Mitigations

### Risk: Network Connectivity Issues
**Mitigation**: Offline mode - cache assigned batches, sync when reconnected

### Risk: Worker Abandonment
**Mitigation**: 60-second heartbeat timeout, auto-reassign stale batches

### Risk: Duplicate Batch Assignment
**Mitigation**: File locking + atomic increment for batch numbers

### Risk: Large Result Sets
**Mitigation**: Stream results to disk, paginated UI display

---

## 📊 Success Metrics

- **Search Speed**: 10x improvement for large ranges (>1M seeds)
- **Participation**: Average 3+ workers per workspace
- **Reliability**: <1% batch assignment conflicts
- **Usability**: 90% of users successfully join workspace on first try

---

## 🛣️ Implementation Roadmap

### Week 1-2: Core Infrastructure
- [ ] Workspace data models
- [ ] Batch coordination logic
- [ ] Local SQLite storage

### Week 3-4: UI Implementation  
- [ ] Create workspace dialog
- [ ] Join workspace flow
- [ ] Active workspace dashboard

### Week 5-6: Integration & Testing
- [ ] Integrate with existing search engine
- [ ] Multi-user testing
- [ ] Performance optimization

### Week 7-8: Polish & Launch
- [ ] Error handling and edge cases
- [ ] Documentation and help system
- [ ] Beta testing with community

---

## 💡 Future Enhancements

- **Smart batch sizing** based on worker performance
- **Priority queues** for high-value seed ranges  
- **Worker reputation** system with contribution tracking
- **Mobile companion** app for monitoring progress
- **Integration** with Discord for notifications

---

This PRD focuses on the **essential collaborative functionality** without over-engineering. The core concept is simple: split work into batches, coordinate assignment, aggregate results. Everything else is polish! 🎯