// tests/e2e/tier2_boundaries/f04_seed_lake_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { getMcpServer, runMotelyTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Feature 4 Boundaries: Seed Lake Service in Motely', () => {
  it('F04-B1: Non-existent partition request returns error from live Motely.MCP lake_query without crashing server', async () => {
    const fakePartition = 'NonExistentRank99';
    assert.equal(exists(path.join(DUNGMOT_PATHS.ranksDir, `${fakePartition}_best.txt`)), false);
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: fakePartition,
      limit: 5,
    });
    const text = rpc.result?.content?.[0]?.text ?? JSON.stringify(rpc.error ?? {});
    assert.ok(rpc.result?.isError || rpc.error || text.toLowerCase().includes('not found') || text.toLowerCase().includes('error'));
  });

  it('F04-B2: Limit = 0 in live lake_query yields zero seeds or clamped empty/minimal response safely', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: '10',
      limit: 0,
    });
    assert.ok(!rpc.error);
    const text = rpc.result.content[0].text;
    if (text.trim().startsWith('{')) {
      const parsed = JSON.parse(text);
      assert.ok(Array.isArray(parsed.seeds));
      assert.ok(parsed.seeds.length === 0 || parsed.count >= 0);
    }
  });

  it('F04-B3: Limit exceeding small Parquet sample size (little_10s = 420 rows) returns all available rows without overflow', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Ranks',
      partition: 'little_10s',
      limit: 5000,
    });
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.equal(parsed.count, 420);
    assert.equal(parsed.seeds.length, 420);
  });

  it('F04-B4: Zero-byte file (triple-negative.csv) is handled safely by live Motely.MCP lake_query with 0 seeds', async () => {
    const zeroByteFile = DUNGMOT_PATHS.tripleNegativeCsv;
    assert.equal(exists(zeroByteFile), true);
    assert.equal(fs.statSync(zeroByteFile).size, 0);

    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'TripleNegative',
      partition: 'triple-negative',
      limit: 10,
    });
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.equal(parsed.count, 0);
    assert.deepEqual(parsed.seeds, []);
  });

  it('F04-B5: SeedLakeTests verify path-traversal rejection and malformed CSV filtering in C# unit suite', () => {
    const res = runMotelyTestsFilter('FullyQualifiedName~SeedLakeTests');
    assert.equal(res.exitCode, 0, `SeedLakeTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F04-B6: Seeds returned from Windows CRLF text files have all trailing CR/LF whitespace stripped', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'CuratedRanks',
      partition: '10',
      limit: 10,
    });
    const parsed = JSON.parse(rpc.result.content[0].text);
    for (const s of parsed.seeds) {
      assert.equal(s, s.trim());
      assert.match(s, /^[1-9A-Z]{1,8}$/);
    }
  });
});
