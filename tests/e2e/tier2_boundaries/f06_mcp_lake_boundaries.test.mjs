// tests/e2e/tier2_boundaries/f06_mcp_lake_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Feature 6 Boundaries: Motely.MCP Lake Tools', () => {
  it('F06-B1: Missing required parameter category in live lake_query returns tool/RPC error without server crash', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', { partition: '10' });
    const text = rpc.result?.content?.[0]?.text ?? '';
    assert.ok(rpc.error || rpc.result?.isError || text.toLowerCase().includes('error') || text.toLowerCase().includes('category'));
  });

  it('F06-B2: Missing required parameter partition in live lake_query returns tool/RPC error without server crash', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', { category: 'Ranks' });
    const text = rpc.result?.content?.[0]?.text ?? '';
    assert.ok(rpc.error || rpc.result?.isError || text.toLowerCase().includes('error') || text.toLowerCase().includes('partition'));
  });

  it('F06-B3: Path traversal characters in partition (../../secret_file) are rejected by SeedLakeService', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: '../../secret_file',
      limit: 5,
    });
    const text = rpc.result?.content?.[0]?.text ?? JSON.stringify(rpc.error ?? {});
    assert.ok(
      rpc.error || rpc.result?.isError || text.toLowerCase().includes('invalid') || text.toLowerCase().includes('error'),
      `Path traversal partition must be rejected, got: ${text}`
    );
  });

  it('F06-B4: Excessive limit parameter (500,000) is clamped safely by Motely.MCP lake_query', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: '10',
      limit: 500000,
    });
    assert.ok(!rpc.error);
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.ok(parsed.seeds.length <= 10000, `Expected clamped seed count <= 10,000, got ${parsed.seeds.length}`);
  });

  it('F06-B5: Unknown category returns clean error response instead of crashing MCP server', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'NonExistentCategory',
      partition: '10',
      limit: 5,
    });
    const text = rpc.result?.content?.[0]?.text ?? JSON.stringify(rpc.error ?? {});
    assert.ok(rpc.error || rpc.result?.isError || text.toLowerCase().includes('unknown') || text.toLowerCase().includes('error') || text.toLowerCase().includes('category'));
  });

  it('F06-B6: Malformed raw JSON POST to /mcp is rejected cleanly with HTTP 4xx or JSON-RPC -32700 parse error', async () => {
    const mcp = await getMcpServer();
    const raw = await mcp.rawMcpPost('{"jsonrpc": "2.0", "id": 99, BROKEN_JSON');
    assert.ok(
      raw.status >= 400 || raw.text.includes('-32700'),
      `Expected HTTP >= 400 or -32700 parse error on malformed JSON, got status=${raw.status}, body=${raw.text}`
    );
  });
});
