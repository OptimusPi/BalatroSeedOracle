# Search Results Modal - Product Requirements Document

## Overview
Create a comprehensive search results modal that displays seed search results in a sortable table view. The modal should be launchable from search widgets and support loading historical results from DuckDB storage.

## User Stories
1. **As a user**, I want to view my search results in a detailed table format so I can analyze seeds effectively
2. **As a user**, I want to launch the results modal from any search widget to see full details
3. **As a user**, I want to load and view past search results from my search history
4. **As a user**, I want to sort results by different columns to find the best seeds quickly
5. **As a user**, I want to export results to CSV for external analysis

## Functional Requirements

### 1. Modal Structure
- [ ] Create `SearchResultsModal` using `StandardModal` as base
- [ ] Full-width modal (95% of screen) matching other modals
- [ ] Balatro-themed styling with modal background (#3a5055)
- [ ] Modal title shows filter name and result count

### 2. Results Table
- [ ] Implement sortable `DataGrid` with following columns:
  - Seed (string)
  - Score (formatted with commas)
  - Antes (comma-separated list)
  - Items Found (visual icons)
  - Timestamp (for historical results)
- [ ] Column headers clickable for sorting
- [ ] Alternating row colors for readability
- [ ] Hover effects on rows
- [ ] Double-click row to copy seed to clipboard

### 3. Widget Integration
- [ ] Add "View Results" button to SearchWidget
- [ ] Button shows result count badge
- [ ] Click opens modal with current results
- [ ] Maintain reference to active search results

### 4. Historical Results Loading
- [ ] Add "Load History" button in modal toolbar
- [ ] Query DuckDB for past search results
- [ ] Display search metadata (filter name, date, duration)
- [ ] Allow switching between different historical searches
- [ ] Show search filter preview for context

### 5. Result Actions
- [ ] Export to CSV functionality
- [ ] Copy all seeds to clipboard
- [ ] Filter/search within results
- [ ] Add to favorites directly from results
- [ ] Delete historical searches

### 6. Performance
- [ ] Virtualized scrolling for large result sets
- [ ] Lazy loading for historical data
- [ ] Pagination or infinite scroll
- [ ] Result caching to prevent re-queries

## Technical Implementation

### Database Schema
```sql
-- Already exists in SearchHistoryService
CREATE TABLE search_results (
    id INTEGER PRIMARY KEY,
    search_id INTEGER,
    seed TEXT,
    score REAL,
    antes TEXT,
    items_json TEXT,
    created_at TIMESTAMP
);
```

### Component Architecture
```
SearchResultsModal
├── ModalHeader
│   ├── Title (filter name + count)
│   ├── HistoryDropdown
│   └── ActionButtons (export, etc)
├── ResultsDataGrid
│   ├── ColumnHeaders (sortable)
│   ├── ResultRows
│   └── VirtualScrollContainer
└── ModalFooter
    ├── ResultStats
    └── CloseButton
```

### Integration Points
1. **SearchWidget.axaml.cs**
   - Add ViewResultsButton
   - Store reference to results
   - Launch modal on click

2. **SearchHistoryService.cs**
   - Add GetSearchResults(searchId) method
   - Add GetRecentSearches() method
   - Handle result pagination

3. **MainWindow.axaml.cs**
   - Handle modal launch requests
   - Manage modal lifecycle

## UI/UX Design

### Visual Design
- Match Balatro's retro gaming aesthetic
- Use color coding:
  - Green (#4BC292) for high scores
  - Yellow (#FEB95F) for medium scores
  - Red (#FE5F55) for low scores
- Icon sprites for found items
- Smooth animations for sorting

### Interactions
- Keyboard shortcuts:
  - `Esc` to close modal
  - `Ctrl+C` to copy selected seed
  - `Ctrl+A` to select all
  - Arrow keys for navigation
- Right-click context menu
- Tooltip previews on hover

## Acceptance Criteria
1. Modal launches from search widget with current results
2. Results display in sortable table format
3. Historical searches load from DuckDB
4. Export to CSV works correctly
5. Performance remains smooth with 10,000+ results
6. All Balatro styling guidelines followed
7. Keyboard navigation fully supported

## Future Enhancements
- Advanced filtering within results
- Result comparison view
- Seed simulator integration
- Share results via link
- Bulk operations on seeds
- Statistical analysis view
- Graph visualizations

## Dependencies
- Avalonia DataGrid control
- DuckDB.NET for database queries
- CsvHelper for export functionality
- Existing SearchHistoryService

## Timeline Estimate
- Modal structure: 2 hours
- DataGrid implementation: 3 hours
- Widget integration: 1 hour
- Historical loading: 2 hours
- Export and actions: 2 hours
- Testing and polish: 2 hours
- **Total: ~12 hours**