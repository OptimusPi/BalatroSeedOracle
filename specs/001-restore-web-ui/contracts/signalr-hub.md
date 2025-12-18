# SignalR Hub Contract: SearchHub

**Endpoint**: `/signalr`  
**Purpose**: Real-time communication for search progress and results

## Hub Methods (Client → Server)

### SubscribeToSearch
**Signature**: `SubscribeToSearch(searchId: string)`  
**Purpose**: Join SignalR group to receive real-time updates for specific search  
**Parameters**:
- `searchId`: Unique identifier for the search session

### UnsubscribeFromSearch  
**Signature**: `UnsubscribeFromSearch(searchId: string)`  
**Purpose**: Leave SignalR group and stop receiving updates for search  
**Parameters**:
- `searchId`: Unique identifier for the search session

## Server Events (Server → Client)

### SearchProgress
**Event**: `SearchProgress`  
**Purpose**: Regular progress updates during search execution  
**Data**:
```json
{
  "progress": 150,
  "total": 1000,
  "seedsSearched": 45000,
  "seedsPerMs": 12.5
}
```

### SeedFound
**Event**: `SeedFound`  
**Purpose**: Immediate notification when matching seed is discovered  
**Data**:
```json
{
  "seed": "ABC12345",
  "score": 150
}
```

### SearchCompleted
**Event**: `SearchCompleted`  
**Purpose**: Notification when search finishes successfully  
**Data**:
```json
{
  "searchId": "search_20251216_101530",
  "totalResults": 25,
  "duration": "00:02:15"
}
```

### SearchStopped
**Event**: `SearchStopped`  
**Purpose**: Notification when search is manually stopped  
**Data**:
```json
{
  "searchId": "search_20251216_101530",
  "reason": "user_stopped"
}
```

### SearchError
**Event**: `SearchError`  
**Purpose**: Error notification if search encounters problems  
**Data**:
```json
{
  "searchId": "search_20251216_101530",
  "error": "Invalid JAML syntax",
  "details": "Line 5: Unknown joker 'InvalidJoker'"
}
```

## Connection Lifecycle

1. **Client connects** to `/signalr` endpoint
2. **Client calls** `SubscribeToSearch(searchId)` to join updates
3. **Server sends** real-time events to subscribed clients
4. **Client calls** `UnsubscribeFromSearch(searchId)` when done
5. **Connection closes** automatically on page unload

## Error Handling

- **Connection failures**: SignalR automatically reconnects
- **Invalid searchId**: Server ignores subscription requests
- **Hub method errors**: Logged server-side, no client notification
- **Event delivery failures**: SignalR handles retry logic