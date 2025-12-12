# DuckDB Research Report for Balatro Seed Oracle

**Date**: December 2025
**Purpose**: Research findings on DuckDB ecosystem, .NET 10/C# 14 features, and future architecture ideas

---

## Current Stack Overview

Balatro Seed Oracle uses:
- `DuckDB.NET.Data.Full` (bundled native binaries)
- Appender pattern for bulk inserts into `SearchResults/<filter>_<deck>_<stake>.db`
- .NET 10, C# 14, SIMD vectorization via Motely engine
- Avalonia UI with Accelerate license

---

## DuckDB Ecosystem Highlights

### DuckLake - Data Sharing Solution (May 2025)

[DuckLake](https://duckdb.org/2025/05/27/ducklake) is a new lakehouse format using SQL + Parquet that solves multi-user data sharing:

**Key Features:**
- Each user runs their own DuckDB instance but shares metadata through a central catalog database
- Catalog can be PostgreSQL, MySQL, SQLite, or DuckDB itself
- `CREATE SHARE` returns a URL others can use to `ATTACH` shared databases
- ACID transactions, time travel, schema evolution across distributed users
- Storage agnostic: Local disk, S3, Azure Blob, GCS

**Seed Farm Potential:**
Users could upload their `.db` files, DuckLake catalogs them with metadata (filter name, deck, stake, seed count, scores), then anyone queries across the entire community's search results.

**Status:** Experimental as of June 2025, expected to mature throughout 2025.

**Resources:**
- [DuckLake GitHub](https://github.com/duckdb/ducklake)
- [DuckLake Docs](https://duckdb.org/docs/stable/core_extensions/ducklake)
- [Getting Started Guide (MotherDuck)](https://motherduck.com/blog/getting-started-ducklake-table-format/)

---

### MotherDuck - Cloud Option

[MotherDuck](https://motherduck.com/) is the serverless cloud warehouse built on DuckDB:

**Features:**
- Hybrid execution: Query runs locally AND in cloud simultaneously
- SHARE/ATTACH features for collaboration
- EU region available (Frankfurt, AWS eu-central-1)
- Free tier available
- MCP server for AI integration

**Use Cases:**
- 24x7 availability for shared seed databases
- Centralized data management
- Team collaboration on seed hunting

**Resources:**
- [MotherDuck Blog](https://motherduck.com/blog/)
- [MCP Server](https://github.com/motherduckdb/mcp-server-motherduck)

---

### DuckDB-WASM - Browser-Based DuckDB

[DuckDB-WASM](https://github.com/duckdb/duckdb-wasm) enables DuckDB in browsers:

**Current Status:**
- Based on DuckDB v1.4.2
- Extensions enabled by default: autocomplete, fts, httpfs, icu, inet, json, parquet, spatial, sqlite_scanner, substrait, tpcds, tpch, vss

**Capabilities:**
- Reads Parquet, CSV, JSON files via Filesystem APIs or HTTP
- Arrow-native data exchange
- Works in Chrome, Firefox, Safari, Node.js

**Limitations:**
- Single-threaded by default (multithreading experimental)
- Sandboxed, limited out-of-core operations
- DuckDB.NET doesn't work with Blazor WASM (would need JS/TypeScript wrapper)

**Potential:** Web-based Balatro Seed Oracle viewer that reads `.db` files client-side.

**Resources:**
- [DuckDB-WASM Releases](https://github.com/duckdb/duckdb-wasm/releases)
- [NPM Package](https://www.npmjs.com/package/@duckdb/duckdb-wasm)
- [Online Shell](https://shell.duckdb.org/)

---

### Other Notable Tools from awesome-duckdb

| Tool | Description | URL |
|------|-------------|-----|
| SQL Workbench | DuckDB-Wasm based editor with visualization | [Link](https://sql-workbench.com/) |
| Tad | Cross-platform tabular data viewer | [GitHub](https://github.com/antonycourtney/tad) |
| Evidence | Generate reports using SQL and markdown | [Link](https://evidence.dev/) |
| Rill Data | Transform datasets into SQL-powered dashboards | [Link](https://www.rilldata.com/) |
| Splink | Data deduplication and record linkage | [GitHub](https://github.com/moj-analytical-services/splink) |

---

## DuckDB.NET Latest Updates

**Current Version:** [DuckDB.NET.Data.Full 1.4.1](https://www.nuget.org/packages/DuckDB.NET.Data.Full) (October 2025)

**Recent Improvements:**
- Updated to DuckDB v1.4.1
- Improved support for parameter binding in parameterized statements
- Sponsored by DuckDB Labs and AWS Open Source Software Fund

### Appender Best Practices

Based on [DuckDB.NET documentation](https://duckdb.net/docs/bulk-data-loading.html):

1. **Data types must match exactly**
   ```csharp
   // WRONG - causes data corruption!
   appender.AppendValue(0);

   // CORRECT for UBIGINT column
   appender.AppendValue(0UL);
   ```

2. **Each Appender should have its own connection** for parallelism
   ```csharp
   // Parallel appends to different tables
   Parallel.ForEach(tables, table => {
       using var conn = new DuckDBConnection(connectionString);
       conn.Open();
       using var appender = conn.CreateAppender(table);
       // ... append data
   });
   ```

3. **Flush periodically** - Appender buffers 204,800 rows before auto-commit
   ```csharp
   // Manual flush for progress visibility
   if (rowCount % 50000 == 0)
       appender.Flush();
   ```

4. **Short-lived appenders** - Create, append, dispose quickly

5. **`AppendDefault()`** - Available since v1.1.0 for auto-increment columns

---

## DuckDB 1.4 LTS New Features

### In-Memory Compression (Opt-in)

```sql
ATTACH ':memory:' AS memory_compressed (COMPRESS);
USE memory_compressed;
```

**5-10x performance boost for some queries!**

**Recommendation:** Consider for quick-search/validation mode in `SearchManager.RunQuickSearchAsync`.

### Rewritten Sorting Engine

- K-way merge sort reduces data movement
- Adaptive to pre-sorted data
- Better thread scaling performance
- New API for use in window functions

### VARIANT Type

Schema-flexible column type for storing arbitrary data:

```sql
CREATE TABLE flexible_data (
    id INTEGER,
    metadata VARIANT
);

INSERT INTO flexible_data VALUES (1, {'filter': 'PerkeoHunt', 'score': 150});
```

**Potential:** Store arbitrary filter metadata without schema changes.

### MERGE Statement

Upsert support for updating existing records:

```sql
MERGE INTO seeds AS target
USING new_seeds AS source
ON target.seed = source.seed
WHEN MATCHED THEN UPDATE SET score = source.score
WHEN NOT MATCHED THEN INSERT VALUES (source.*);
```

**Potential:** Update existing seed results rather than append-only.

### Vortex File Format (v1.4.2)

New columnar format via `vortex` extension:
- Available on Linux and macOS
- Might be faster than Parquet for certain workloads

### Other Performance Improvements

- WAL index now buffers deletes, not only appends
- Encryption performance: Use `LOAD httpfs` for hardware-accelerated OpenSSL

---

## .NET 10 / C# 14 New Features

Based on [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14):

### File-Based Apps (Script Mode)

```csharp
// myutil.cs - Run directly: dotnet run myutil.cs
#:package DuckDB.NET.Data.Full

using DuckDB.NET.Data;

var conn = new DuckDBConnection("Data Source=:memory:");
conn.Open();
Console.WriteLine("DuckDB script mode!");
```

**Potential:** Quick utility scripts for database maintenance.

### Field-Backed Properties

```csharp
// Before
private string _name;
public string Name { get => _name; set => _name = value?.Trim() ?? ""; }

// After (C# 14)
public string Name { get => field; set => field = value?.Trim() ?? ""; }
```

### Extension Members (Beyond Methods)

```csharp
extension DuckDBExtensions for DuckDBConnection
{
    public bool IsOpen => this.State == ConnectionState.Open;
    public static string DefaultConnectionString => "Data Source=:memory:";
}
```

### Null-Conditional Assignment

```csharp
// Only assigns if searchInstance is not null
searchInstance?.Status = SearchStatus.Cancelled;
```

### Span Conversions

New implicit conversions between span types for better performance.

---

## Avalonia DataGrid Options

### TreeDataGrid (Accelerate Component)

Since you have Avalonia Accelerate, [TreeDataGrid](https://docs.avaloniaui.net/docs/reference/controls/treedatagrid/) is available:

**Features:**
- Virtualized for thousands of rows
- Flat or hierarchical modes
- Built-in sorting with click headers
- Selection modes: single/multi, row/cell
- Column types: Text, Checkbox, Hierarchical, Template

**Column Types:**
```csharp
new FlatTreeDataGridSource<SeedResult>(results)
{
    Columns =
    {
        new TextColumn<SeedResult, string>("Seed", x => x.Seed),
        new TextColumn<SeedResult, int>("Score", x => x.Score),
        new CheckBoxColumn<SeedResult>("Favorite", x => x.IsFavorite),
        new TemplateColumn<SeedResult>("Actions", "ActionTemplate"),
    }
};
```

### DataTable Binding Workaround

Avalonia DataGrid doesn't support `DataTable` directly:

```csharp
// Workaround
dataGrid.ItemsSource = dataTable.AsDataView();

foreach (DataColumn column in dataTable.Columns)
{
    dataGrid.Columns.Add(new DataGridTextColumn
    {
        Header = column.ColumnName,
        Binding = new Binding($"Row.ItemArray[{column.Ordinal}]")
    });
}
```

**Recommendation:** Map DuckDB results to `ObservableCollection<YourResultModel>` instead.

---

## Immediate Action Items

### 1. Version Pinning (Optional)

Current `.csproj` doesn't pin versions. For stability:

```xml
<PackageReference Include="DuckDB.NET.Data.Full" Version="1.4.1" />
```

### 2. In-Memory Compression for Quick Search

In `SearchManager.RunQuickSearchAsync`, if using `:memory:`:

```csharp
connection.Execute("ATTACH ':memory:' AS quick_compressed (COMPRESS);");
connection.Execute("USE quick_compressed;");
```

Could give 5-10x speedup for validation queries.

### 3. Appender Data Type Audit

Review all `AppendValue` calls to ensure exact type matching:
- `int` for INTEGER
- `long` for BIGINT
- `ulong` for UBIGINT
- `double` for DOUBLE
- `string` for VARCHAR

---

## Future Architecture: Balatro Seed Farm

```
┌─────────────────────────────────────────────────────────────┐
│                    BALATRO SEED FARM                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  User A (Desktop)          User B (Desktop)                 │
│  ┌─────────────┐           ┌─────────────┐                  │
│  │ BSO App     │           │ BSO App     │                  │
│  │ ┌─────────┐ │           │ ┌─────────┐ │                  │
│  │ │ DuckDB  │ │           │ │ DuckDB  │ │                  │
│  │ │ Local   │ │           │ │ Local   │ │                  │
│  │ └─────────┘ │           │ └─────────┘ │                  │
│  └──────┬──────┘           └──────┬──────┘                  │
│         │ Upload .db              │ Upload .db              │
│         ▼                         ▼                         │
│  ┌─────────────────────────────────────────┐                │
│  │         DuckLake Catalog                │                │
│  │  (PostgreSQL / MotherDuck / SQLite)     │                │
│  │                                         │                │
│  │  Tables:                                │                │
│  │  - seed_files (user, filter, deck...)  │                │
│  │  - community_seeds (aggregated)         │                │
│  └─────────────────────────────────────────┘                │
│                         │                                   │
│                         ▼                                   │
│  ┌─────────────────────────────────────────┐                │
│  │         Object Storage (S3/Azure)       │                │
│  │         Parquet files from uploads      │                │
│  └─────────────────────────────────────────┘                │
│                         │                                   │
│                         ▼                                   │
│  ┌─────────────────────────────────────────┐                │
│  │         Web Viewer (DuckDB-WASM)        │                │
│  │         Query community seeds           │                │
│  └─────────────────────────────────────────┘                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Phases

**Phase 1: Export to Parquet**
- Add "Export to Parquet" option for `.db` files
- Parquet is the interchange format for DuckLake

**Phase 2: DuckLake Catalog**
- Set up catalog database (start with SQLite for simplicity)
- Define schema for seed file metadata

**Phase 3: Upload/Download**
- Simple file upload to object storage
- Register in DuckLake catalog

**Phase 4: Web Viewer**
- DuckDB-WASM based query interface
- Read community Parquet files via HTTP

---

## Sources

- [DuckDB.NET NuGet 1.4.1](https://www.nuget.org/packages/DuckDB.NET.Data/)
- [DuckDB.NET Documentation](https://duckdb.net/)
- [DuckLake Announcement](https://duckdb.org/2025/05/27/ducklake)
- [DuckLake GitHub](https://github.com/duckdb/ducklake)
- [DuckDB 1.4.0 Release](https://duckdb.org/2025/09/16/announcing-duckdb-140)
- [DuckDB 1.4.2 LTS](https://duckdb.org/2025/11/12/announcing-duckdb-142)
- [DuckDB Configuration](https://duckdb.org/docs/stable/configuration/overview)
- [DuckDB Performance Guide](https://duckdb.org/docs/stable/guides/performance/overview)
- [DuckDB-WASM GitHub](https://github.com/duckdb/duckdb-wasm)
- [MotherDuck](https://motherduck.com/)
- [MotherDuck Ecosystem Blog](https://motherduck.com/blog/duckdb-ecosystem-newsletter-november-2025/)
- [Avalonia TreeDataGrid Docs](https://docs.avaloniaui.net/docs/reference/controls/treedatagrid/)
- [Avalonia DataGrid Docs](https://docs.avaloniaui.net/docs/reference/controls/datagrid/)
- [DuckDB.NET Bulk Loading](https://duckdb.net/docs/bulk-data-loading.html)
- [C# 14 What's New](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [.NET 10 Announcement](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [awesome-duckdb](https://github.com/davidgasquez/awesome-duckdb)
