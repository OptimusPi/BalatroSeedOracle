# Balatro Seed Oracle MCP Server

A Model Context Protocol (MCP) server for Balatro seed filtering with JAML schema support.

## Features

- **JAML Schema Validation**: Validates JAML filters against the official schema
- **Filter Management**: Create, list, and manage JAML filters
- **Seed Source Discovery**: List available DuckDB, CSV, and TXT seed files
- **Template Generation**: Generate JAML templates for common patterns
- **Smart Path Resolution**: Supports both absolute and relative paths

## MCP Tools

### `list_filters`
Lists all available JAML filters in the `JamlFilters/` directory.

```json
{
  "name": "list_filters",
  "description": "List all available JAML filters"
}
```

### `list_seed_sources`
Lists all available seed sources (DuckDB, CSV, TXT files) in the `SeedSources/` directory.

```json
{
  "name": "list_seed_sources", 
  "description": "List all available seed sources (DuckDB, CSV, TXT files)"
}
```

### `create_filter`
Creates a new JAML filter with validation.

```json
{
  "name": "create_filter",
  "description": "Create a new JAML filter with validation",
  "inputSchema": {
    "type": "object",
    "properties": {
      "name": {"type": "string", "description": "Filter name"},
      "description": {"type": "string", "description": "Filter description"},
      "jamlContent": {"type": "string", "description": "JAML content following the schema"}
    },
    "required": ["name", "jamlContent"]
  }
}
```

### `validate_jaml`
Validates JAML content against the schema.

```json
{
  "name": "validate_jaml",
  "description": "Validate JAML content against the schema",
  "inputSchema": {
    "type": "object", 
    "properties": {
      "jamlContent": {"type": "string", "description": "JAML content to validate"}
    },
    "required": ["jamlContent"]
  }
}
```

### `get_jaml_schema`
Gets the JAML schema for reference.

```json
{
  "name": "get_jaml_schema",
  "description": "Get the JAML schema for reference"
}
```

### `generate_template`
Generates JAML templates for common patterns.

```json
{
  "name": "generate_template",
  "description": "Generate JAML templates for common patterns",
  "inputSchema": {
    "type": "object",
    "properties": {
      "templateType": {
        "type": "string",
        "enum": ["basic", "joker", "deck", "voucher", "complex"],
        "description": "Type of template to generate"
      }
    },
    "required": ["templateType"]
  }
}
```

## Usage

### Building

```bash
cd src/BalatroSeedOracle.MCP.CLI
dotnet build
```

### Running

```bash
dotnet run
```

The MCP server communicates via stdio using JSON-RPC 2.0 protocol.

### Example MCP Client Usage

```bash
# List available filters
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"list_filters","arguments":{}}}' | dotnet run

# Create a new filter
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"create_filter","arguments":{"name":"MyFilter","description":"Test filter","jamlContent":"must:\n  - type: joker\n    name: Blueprint\nshould:\n  - type: playing_card\n    rank: A\n    score: 1"}}}' | dotnet run

# Validate JAML
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"validate_jaml","arguments":{"jamlContent":"must:\n  - type: joker\n    name: Blueprint"}}}' | dotnet run
```

## Directory Structure

```
BalatroSeedOracle/
├── JamlFilters/          # JAML filter files
├── SeedSources/          # DuckDB, CSV, TXT seed files
├── jaml.schema.json      # JAML schema definition
└── src/
    └── BalatroSeedOracle.MCP.CLI/
        ├── Program.cs
        ├── MCP/
        │   ├── BalatroMcpServer.cs
        │   ├── McpServerHost.cs
        │   └── Protocol/
        │       └── BalatroMcpProtocol.cs
        └── BalatroSeedOracle.MCP.CLI.csproj
```

## Integration with AI Assistants

This MCP server can be used with AI assistants that support the Model Context Protocol, such as Claude Desktop, to:

1. **Create JAML filters** based on natural language descriptions
2. **Validate** JAML syntax against the schema
3. **Generate templates** for common filtering patterns
4. **Manage** existing filters and seed sources

The server provides structured access to Balatro's filtering capabilities while maintaining schema compliance.
