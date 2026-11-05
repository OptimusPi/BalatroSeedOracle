// tests/e2e/tier1_features/f06_mcp_lake_tools.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Feature 6: Motely.MCP Lake Tools', () => {
  it('F06-1: Live MCP tools/list exposes lake_list with optional category inputSchema', async () => {
    const mcp = await getMcpServer();
    const tools = await mcp.listTools();
    const lakeList = tools.find(t => t.name === 'lake_list');
    assert.ok(lakeList, 'lake_list tool must be registered on Motely.MCP');
    assert.ok(lakeList.description && lakeList.description.length > 10);
    assert.equal(lakeList.inputSchema.type, 'object');
    assert.ok(lakeList.inputSchema.properties?.category, 'lake_list schema must define optional category');
  });

  it('F06-2: Live MCP tools/list exposes lake_query requiring category and partition', async () => {
    const mcp = await getMcpServer();
    const tools = await mcp.listTools();
    const lakeQuery = tools.find(t => t.name === 'lake_query');
    assert.ok(lakeQuery, 'lake_query tool must be registered on Motely.MCP');
    assert.ok(lakeQuery.inputSchema.required?.includes('category'));
    assert.ok(lakeQuery.inputSchema.required?.includes('partition'));
  });

  it('F06-3: Live lake_query returns real seeds as structured JSON text content', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Ranks',
      partition: 'little_10s',
      limit: 3,
    });
    assert.equal(rpc.result.content[0].type, 'text');
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.equal(parsed.count, 3);
    assert.equal(parsed.seeds.length, 3);
    for (const s of parsed.seeds) {
      assert.match(s, /^[1-9A-Z]{1,8}$/);
    }
  });

  it('F06-4: Live MCP seed_search tool executes against NegativeUncommon lake partition', async () => {
    const mcp = await getMcpServer();
    const tools = await mcp.listTools();
    const searchTool = tools.find(t => t.name === 'seed_search');
    assert.ok(searchTool, 'seed_search tool must be registered on Motely.MCP');

    const rpc = await mcp.callTool('seed_search', {
      filterOrJaml: 'name: LakeOverstock\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n',
      mode: 'lake',
      lakeCategory: 'NegativeUncommon',
      lakePartition: 'uncommonss',
      limit: 5,
    });
    assert.equal(rpc.jsonrpc, '2.0');
    assert.ok(!rpc.error, `Unexpected JSON-RPC error: ${JSON.stringify(rpc.error)}`);
    const text = rpc.result.content[0].text;
    assert.ok(text.length > 0);
  });

  it('F06-5: Live lake_list returns real partition metadata with estimatedSeedCount and isParquet flags', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_list', { category: 'Ranks' });
    const parsed = JSON.parse(rpc.result.content[0].text);
    const items = parsed.lakes ?? parsed.partitions ?? parsed;
    const little10s = items.find(p => (p.partition ?? p.Partition) === 'little_10s');
    assert.ok(little10s, 'little_10s must appear in Ranks lake_list');
    assert.equal(little10s.isParquet ?? little10s.IsParquet, true);
    assert.ok((little10s.estimatedSeedCount ?? little10s.EstimatedSeedCount) > 0);
  });

  it('F06-6: MCP initialize and tools/call responses conform to JSON-RPC 2.0 specification', async () => {
    const mcp = await getMcpServer();
    assert.equal(mcp.initJson.jsonrpc, '2.0');
    assert.ok(mcp.initJson.result?.serverInfo?.name);
    const rpc = await mcp.callTool('lake_list', {});
    assert.equal(rpc.jsonrpc, '2.0');
    assert.ok(Array.isArray(rpc.result?.content));
  });
});
