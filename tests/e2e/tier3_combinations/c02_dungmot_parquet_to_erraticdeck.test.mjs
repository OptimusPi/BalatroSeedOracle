// tests/e2e/tier3_combinations/c02_dungmot_parquet_to_erraticdeck.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { DUNGMOT_PATHS, PATHS, exists } from '../helpers/env.mjs';
import { validateParquetFile, readParquetMetadata } from '../helpers/parquet_oracle.mjs';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Combination C02: dungmot Parquet Lake <-> ErraticDeck.app DuckDB-WASM Fallback', () => {
  it('C02-1: Little 10s Parquet file exists in dungmot and satisfies Parquet PAR1 magic + Thrift footer', () => {
    const parquetFile = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    const result = validateParquetFile(parquetFile);
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.footerLength > 50);
  });

  it('C02-2: ErraticDeck.app API route and parquetSipQuery handle local dungmot parquet_lake fallback', () => {
    const routeFile = path.join(PATHS.ErraticDeckApp, 'app', 'api', 'ranks', '[rank]', 'route.ts');
    const sipFile = path.join(PATHS.ErraticDeckApp, 'lib', 'parquetSipQuery.ts');
    assert.equal(exists(routeFile), true);
    assert.equal(exists(sipFile), true);
    assert.ok(fs.readFileSync(routeFile, 'utf-8').includes('D:/dungmot/parquet_lake/ranks/'));
    assert.ok(fs.readFileSync(sipFile, 'utf-8').includes('union_by_name = true'));
  });

  it('C02-3: Real Thrift FileMetaData schema of little_10s.parquet decodes columns (seed, category, partition)', () => {
    const parquetFile = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    const meta = readParquetMetadata(parquetFile);
    assert.deepEqual(meta.columns, ['seed', 'category', 'partition']);
    assert.ok(meta.numRows > 0);
  });

  it('C02-4: All 13 rank partitions have valid Parquet sample files with matching Thrift schemas', () => {
    const ranks = ['2s', '3s', '4s', '5s', '6s', '7s', '8s', '9s', '10s', 'Jacks', 'Queens', 'Kings', 'Aces'];
    for (const rank of ranks) {
      const p = path.join(DUNGMOT_PATHS.parquetRanks, `little_${rank}.parquet`);
      assert.equal(exists(p), true, `Sample parquet for ${rank} missing at ${p}`);
      const meta = readParquetMetadata(p);
      assert.deepEqual(meta.columns, ['seed', 'category', 'partition']);
      assert.ok(meta.numRows > 0);
    }
  });

  it('C02-5: Real Parquet ZSTD row decoding via Motely.MCP lake_query returns valid Balatro seeds', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Ranks',
      partition: 'little_10s',
      limit: 5,
    });
    const payload = JSON.parse(rpc.result.content[0].text);
    assert.equal(payload.category, 'Ranks');
    assert.equal(payload.partition, 'little_10s');
    assert.equal(payload.seeds.length, 5);
    for (const seed of payload.seeds) {
      assert.match(seed, /^[1-9A-Z]{1,8}$/);
    }
  });
});
