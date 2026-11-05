// tests/e2e/tier3_combinations/c08_motely_mcp_to_dungmot_lake.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Combination C08: Motely.MCP Lake Tools <-> dungmot Seed Lake', () => {
  it('C08-1: Live lake_list MCP tool discovers Ranks Parquet partitions in dungmot parquet_lake', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_list', { category: 'Ranks' });
    const data = JSON.parse(rpc.result.content[0].text);
    const items = data.lakes ?? data.partitions ?? data;
    assert.ok(Array.isArray(items), 'lake_list must return lakes array');
    assert.ok(items.some(p => (p.partition ?? p.Partition ?? p.name ?? p.Name) === 'little_10s'));
  });

  it('C08-2: Live lake_list MCP tool discovers CuratedRanks and CuratedSuits text lists', async () => {
    const mcp = await getMcpServer();
    const ranksRpc = await mcp.callTool('lake_list', { category: 'CuratedRanks' });
    const suitsRpc = await mcp.callTool('lake_list', { category: 'CuratedSuits' });
    const ranks = JSON.parse(ranksRpc.result.content[0].text);
    const suits = JSON.parse(suitsRpc.result.content[0].text);
    assert.ok((ranks.lakes ?? ranks).length >= 13);
    assert.ok((suits.lakes ?? suits).length >= 4);
  });

  it('C08-3: Live lake_list MCP tool discovers NegativeUncommon seed catalog', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_list', { category: 'NegativeUncommon' });
    const parsed = JSON.parse(rpc.result.content[0].text);
    const items = parsed.lakes ?? parsed.partitions ?? parsed;
    assert.ok(items.length >= 1);
  });

  it('C08-4: Live lake_query MCP tool reads real seeds from CuratedRanks 10 without file lock', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: '10',
      limit: 5,
    });
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.equal(parsed.count, 5);
    assert.equal(parsed.seeds[0], '1UQ8D22L');
  });

  it('C08-5: MCP lake tools return standard JSON-RPC 2.0 tool execution envelopes with structured text', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedSuits',
      partition: 'Spades',
      limit: 3,
    });
    assert.equal(rpc.jsonrpc, '2.0');
    assert.equal(rpc.result.content[0].type, 'text');
    const body = JSON.parse(rpc.result.content[0].text);
    assert.equal(body.seeds[0], '113L2V3N');
  });
});
