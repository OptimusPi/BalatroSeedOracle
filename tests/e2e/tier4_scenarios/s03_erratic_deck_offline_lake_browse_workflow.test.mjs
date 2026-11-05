// tests/e2e/tier4_scenarios/s03_erratic_deck_offline_lake_browse_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { PATHS, DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { readParquetMetadata, validateSeedListFile } from '../helpers/parquet_oracle.mjs';
import {
  getMcpServer,
  getMotelyWasm,
  analyzeSeedWithWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Tier 4 Scenario 3: Erratic Deck Offline Parquet Analysis Workflow', () => {
  it('Step 1: ErraticDeck.app production build output and parquetLake helpers load cleanly', async () => {
    assert.equal(exists(path.join(PATHS.ErraticDeckApp, '.next')), true);
    const lakeMod = await import(pathToFileURL(path.join(PATHS.ErraticDeckApp, 'lib', 'parquetLake.ts')).href);
    assert.equal(lakeMod.STANDARD_RANKS.length, 13);
    assert.equal(lakeMod.getParquetApiUrl('little_10s', 'http://localhost:3000'), 'http://localhost:3000/api/ranks/little_10s');
  });

  it('Step 2: Local Parquet file little_10s.parquet contains real Thrift schema and >0 rows', () => {
    const parquetFile = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    const meta = readParquetMetadata(parquetFile);
    assert.deepEqual(meta.columns, ['seed', 'category', 'partition']);
    assert.ok(meta.numRows > 0, `Expected >0 rows in little_10s.parquet, got ${meta.numRows}`);
  });

  it('Step 3: Live Parquet reader decodes real ZSTD seed rows from little_10s.parquet via Motely.MCP', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Ranks',
      partition: 'little_10s',
      limit: 5,
    });
    const payload = JSON.parse(rpc.result.content[0].text);
    assert.equal(payload.count, 5);
    assert.equal(payload.seeds.length, 5);
    for (const seed of payload.seeds) {
      assert.match(seed, /^[1-9A-Z]{1,8}$/);
    }
  });

  it('Step 4: Curated 10_best.txt top seed 1UQ8D22L analyzes in motely-wasm with >= 18 Tens in 52-card Erratic Deck', async () => {
    const listCheck = validateSeedListFile(path.join(DUNGMOT_PATHS.ranksDir, '10_best.txt'), 50);
    assert.equal(listCheck.valid, true);
    const topSeed = listCheck.seeds[0];

    const { MotelyStandardcardRank } = await getMotelyWasm();
    const res = await analyzeSeedWithWasm(topSeed, { deck: 'Erratic', stake: 'White' });
    assert.ok(res.erraticDeck, 'Erratic deck must populate 52 starting cards');
    assert.equal(res.erraticDeck.length, 52);

    let tens = 0;
    for (const card of res.erraticDeck) {
      if (card.standardcardRank === MotelyStandardcardRank.Ten) tens++;
    }
    assert.ok(tens >= 18, `Expected curated 10_best seed ${topSeed} to have >= 18 Tens, got ${tens}`);
  });

  it('Step 5: Built jaml-ui stylesheet jimbo.css and JamlyzerErraticDeck component exist in dist/', () => {
    assert.equal(exists(path.join(PATHS.jamlUi, 'dist', 'ui', 'jimbo.css')), true);
    assert.equal(exists(path.join(PATHS.jamlUi, 'dist', 'components', 'jamlyzer', 'JamlyzerErraticDeck.js')), true);
  });
});
