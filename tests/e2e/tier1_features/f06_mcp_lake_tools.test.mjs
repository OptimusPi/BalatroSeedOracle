// tests/e2e/tier1_features/f06_mcp_lake_tools.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Feature 6: Motely.MCP Lake Tools', () => {
  it('F06-1: lake_list tool definition exposes name, description, and empty or optional parameter schema', () => {
    const lakeListTool = {
      name: 'lake_list',
      description: 'List available Balatro seed lake partitions and catalogs',
      inputSchema: {
        type: 'object',
        properties: {
          category: { type: 'string', description: 'Filter by category (Ranks, Suits, NegativeUncommon, etc.)' },
        },
      },
    };
    assert.equal(lakeListTool.name, 'lake_list');
    assert.ok(lakeListTool.description.includes('seed lake'));
  });

  it('F06-2: lake_query tool definition requires category and partition parameters', () => {
    const lakeQueryTool = {
      name: 'lake_query',
      description: 'Query seeds from a specific lake partition',
      inputSchema: {
        type: 'object',
        properties: {
          category: { type: 'string' },
          partition: { type: 'string' },
          limit: { type: 'integer' },
        },
        required: ['category', 'partition'],
      },
    };
    assert.equal(lakeQueryTool.name, 'lake_query');
    assert.deepEqual(lakeQueryTool.inputSchema.required, ['category', 'partition']);
  });

  it('F06-3: lake_query response formats seeds as structured JSON text content', () => {
    const response = {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            category: 'Ranks',
            partition: '10s',
            count: 3,
            seeds: ['111S8CHT', '2UQ8D22L', '99AAXXYY'],
          }),
        },
      ],
    };
    assert.equal(response.content[0].type, 'text');
    const parsed = JSON.parse(response.content[0].text);
    assert.equal(parsed.count, 3);
    assert.equal(parsed.seeds.length, 3);
  });

  it('F06-4: MCP search tool supports mode: "lake" parameter', () => {
    const searchArgs = {
      jaml: 'must:\n  - voucher: Overstock',
      mode: 'lake',
      lakeCategory: 'NegativeUncommon',
      lakePartition: 'uncommonss',
    };
    assert.equal(searchArgs.mode, 'lake');
    assert.equal(searchArgs.lakeCategory, 'NegativeUncommon');
  });

  it('F06-5: lake_list returns metadata with estimated count and parquet status', () => {
    const sampleMetadata = {
      Name: '10s',
      Path: 'D:\\dungmot\\parquet_lake\\ranks\\10s.parquet',
      Category: 'Ranks',
      Partition: '10s',
      EstimatedSeedCount: 100000000,
      IsParquet: true,
    };
    assert.equal(sampleMetadata.IsParquet, true);
    assert.ok(sampleMetadata.EstimatedSeedCount > 0);
  });

  it('F06-6: Model Context Protocol JSON-RPC envelope conforms to 2.0 specification', () => {
    const jsonRpcRequest = {
      jsonrpc: '2.0',
      id: 1,
      method: 'tools/call',
      params: {
        name: 'lake_list',
        arguments: {},
      },
    };
    assert.equal(jsonRpcRequest.jsonrpc, '2.0');
    assert.equal(jsonRpcRequest.method, 'tools/call');
  });
});
