# Motely API Routes Reference

**Base URL**: `http://localhost:3141` (or your configured port)

## Core Endpoints

### Health & Info
- **GET** `/health` - Health check
  - Returns: `{ status: "healthy", timestamp: DateTime }`

- **GET** `/routes` - List all available routes
  - Returns: Object with all route descriptions

### Seed Analysis
- **GET** `/analyze?seed=SEED&deck=Red&stake=White`
  - Query params:
    - `seed` (required) - The seed string (e.g., "MO4E11BR")
    - `deck` (optional) - Deck name (default: "Red")
    - `stake` (optional) - Stake level (default: "White")
  - Returns: Plain text analysis of the seed

### Filters
- **GET** `/filters` - Get all filters
  - Returns: Array of filter objects

- **POST** `/filters/update` - Save/update a filter
  - Body: `{ filterId?: string, filterJaml: string, createNew?: boolean }`
  - Returns: `{ filePath: string }`

- **DELETE** `/filters/{id}` - Delete a filter
  - Path param: `id` - Filter filename (without .jaml extension)
  - Returns: 200 OK or 404 Not Found

### Seed Sources
- **GET** `/seed-sources` - Get available seed sources
  - Returns: `{ sources: Array<{ key: string, label: string, kind: string, fileName?: string }> }`

### Searches
- **GET** `/searches` - Get all active searches
  - Returns: `{ searches: Array<SearchStatus> }`
  - SearchStatus includes: `id`, `searchId`, `filterName`, `deck`, `stake`, `completedBatches`, `totalBatches`, `seedsSearched`, `seedsPerSecond`, `resultsFound`, `isRunning`, `isFastLane`, `inQueue`, `stopReason`

- **POST** `/search` - Start a new search
  - Body: `SearchStartRequest`
    ```json
    {
      "filterId": "string",      // Filter filename (without .jaml)
      "deck": "string",          // Optional, default: "Red"
      "stake": "string",         // Optional, default: "White"
      "startBatch": number,      // Optional, batch number to start from
      "cutoff": number,          // Optional, result limit
      "seedSource": "string"     // Optional, seed source key
    }
    ```
  - Returns: `{ searchId: string, status: "running", columns: string[] }`

- **GET** `/search/{id}` - Get search status and results
  - Path param: `id` - Search ID
  - Returns: 
    ```json
    {
      "searchId": "string",
      "status": "running" | "stopped",
      "results": SearchResult[],
      "progressPercent": number,
      "columns": string[]
    }
    ```

- **POST** `/search/stop` - Stop a running search
  - Body: `{ searchId?: string }` (optional, stops all if not provided)
  - Returns: `{ message: "Search stopped", results: SearchResult[], isBackgroundRunning: false }`

### MCP (AI Generation) Endpoints

- **POST** `/mcp/prompt` - Generate JAML filter and run search from prompt
  - Body: `{ prompt: string }`
  - Returns:
    ```json
    {
      "success": boolean,
      "jamlFilter": string,
      "reasoning": string,
      "error": string | null,
      "searchId": string,
      "results": SearchResult[],
      "columns": string[],
      "message": string,
      "searchUrl": string
    }
    ```

- **POST** `/mcp/generate` - Generate JAML filter only (no search)
  - Body: `{ prompt: string }`
  - Returns:
    ```json
    {
      "success": boolean,
      "jaml": string,
      "reasoning": string,
      "error": string | null
    }
    ```

- **POST** `/mcp` - JSON-RPC 2.0 protocol endpoint for AI assistants
  - Body: JSON-RPC 2.0 request
  - Returns: JSON-RPC 2.0 response

## SignalR Hub

- **Hub URL**: `/searchHub`
  - Real-time search updates via SignalR
  - Connect to receive live search progress and results

## Request/Response Types

### SearchStartRequest
```typescript
{
  filterId?: string;
  deck?: string;
  stake?: string;
  seedCount?: number;
  startBatch?: number;
  cutoff?: number;
  seedSource?: string;
}
```

### SearchStopRequest
```typescript
{
  searchId?: string;
}
```

### FilterSaveRequest
```typescript
{
  filterId?: string;
  filterJaml: string;
  createNew?: boolean;
}
```

### SearchCriteriaDto (for reference - used in some internal APIs)
```typescript
{
  threadCount?: number;        // Default: CPU count
  batchSize?: number;          // Default: 2 (35^2 seeds per batch)
  deck?: string;               // Red, Blue, Yellow, Ghost, etc.
  stake?: string;              // White, Red, Green, Black, Blue, Purple, Orange, Gold
  minScore?: number;           // Default: 0
  startBatch?: number;         // Default: 0
  endBatch?: number;           // Default: ulong.MaxValue
  sourceType?: string;         // single, wordlist, dblist, etc.
}
```

### McpPromptRequest
```typescript
{
  prompt: string;
}
```

### SearchResult
Array of objects with dynamic columns based on the filter. Common columns include:
- `Seed` - The seed string
- `Ante` - Ante number
- `Joker` - Joker name
- `Tag` - Tag name
- And other filter-specific columns

## Example Usage

### Start a search
```javascript
const response = await fetch('http://localhost:3141/search', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    filterId: 'myfilter',
    deck: 'Ghost',
    stake: 'Red',
    seedCount: 1000000
  })
});
const { searchId } = await response.json();
```

### Check search status
```javascript
const response = await fetch(`http://localhost:3141/search/${searchId}`);
const { status, results, progressPercent } = await response.json();
```

### Get all filters
```javascript
const response = await fetch('http://localhost:3141/filters');
const filters = await response.json();
```

### Analyze a seed
```javascript
const response = await fetch('http://localhost:3141/analyze?seed=MO4E11BR&deck=Ghost&stake=Red');
const analysis = await response.text(); // Returns plain text
```

## CORS

All endpoints support CORS with `AllowAll` policy (any origin, method, header).

## Notes

- All endpoints return JSON except `/analyze` which returns plain text
- Error responses follow format: `{ error: string }`
- Search results are paginated/streamed via SignalR for real-time updates
- Filter IDs are filenames without the `.jaml` extension
