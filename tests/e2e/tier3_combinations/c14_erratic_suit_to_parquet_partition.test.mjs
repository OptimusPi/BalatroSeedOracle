// tests/e2e/tier3_combinations/c14_erratic_suit_to_parquet_partition.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import {
  readParquetMetadata,
  validateSeedListFile,
} from '../helpers/parquet_oracle.mjs';
import {
  getMcpServer,
  analyzeSeedWithWasm,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C14: dungmot Suit Parquet Partitions <-> ErraticDeck Suit Concentration', () => {
  it('C14-1: All 4 suit sample Parquet files have valid Thrift footers, positive row counts, and seed columns', () => {
    const suits = ['Clubs', 'Diamonds', 'Hearts', 'Spades'];
    for (const suit of suits) {
      const p = path.join(DUNGMOT_PATHS.parquetSuits, `little_${suit}.parquet`);
      assert.equal(exists(p), true, `Parquet for suit ${suit} missing at ${p}`);
      const meta = readParquetMetadata(p);
      assert.ok(meta.numRows > 0, `Expected positive numRows in little_${suit}.parquet`);
      assert.ok(meta.columns.includes('seed'), `Expected 'seed' column in ${meta.columns.join(',')}`);
    }
  });

  it('C14-2: Curated suits best lists exist and contain valid Balatro seeds for all 4 suits', () => {
    const files = ['Club_best.txt', 'Diamond_best.txt', 'Heart_best.txt', 'Spades_best.txt'];
    for (const file of files) {
      const p = path.join(DUNGMOT_PATHS.suitsDir, file);
      const val = validateSeedListFile(p, 10);
      assert.equal(val.valid, true, val.reason);
    }
  });

  it('C14-3: Top curated Spades seed 113L2V3N genuinely achieves >= 25 Spades in real WASM Erratic Deck simulation', async () => {
    const res = await runWasmSeedSearch(
      'deck: Erratic\nstake: White\nmust:\n  - erraticSuit: Spades\n    min: 25\n',
      ['113L2V3N', '11111111']
    );
    assert.ok(res.matches.includes('113L2V3N'), 'Curated Spades seed 113L2V3N must have >= 25 Spades');
    assert.ok(!res.matches.includes('11111111'), 'Control seed 11111111 should not have >= 25 Spades');
  });

  it('C14-4: Live Motely.MCP lake_query decodes ZSTD rows from Suits Parquet partition little_Spades', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Suits',
      partition: 'little_Spades',
      limit: 5,
    });
    const body = JSON.parse(rpc.result.content[0].text);
    assert.equal(body.count, 5);
    for (const s of body.seeds) {
      assert.match(s, /^[1-9A-Z]{1,8}$/);
    }
  });

  it('C14-5: MotelyJamlyzer WASM analysis on Spades_best top seed inspects all 52 Erratic playing cards', async () => {
    const { getMotelyWasm } = await import('../helpers/runtime_oracle.mjs');
    const { MotelyStandardcardSuit } = await getMotelyWasm();
    const jam = await analyzeSeedWithWasm('113L2V3N', { deck: 'Erratic', stake: 'White' });
    assert.ok(!jam.error);
    assert.equal(jam.erraticDeck.length, 52);
    const spadeCards = jam.erraticDeck.filter(c => c.standardcardSuit === MotelyStandardcardSuit.Spades);
    assert.ok(spadeCards.length >= 25, `Expected >= 25 Spades in Jamlyzer erraticDeck, got ${spadeCards.length}`);
  });
});
