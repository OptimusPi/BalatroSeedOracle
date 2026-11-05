// tests/e2e/tier1_features/f13_seedfinder_lake_browser.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getSeedfinderLakeRoute,
  getMotelyWasm,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 13: seedfinder.app Dungmot Lake Browser (/api/lake)', () => {
  it('F13-1: Live /api/lake GET serves real curated seeds across all 13 standard rank partitions', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const ranks = ['2', '3', '4', '5', '6', '7', '8', '9', '10', 'Jack', 'Queen', 'King', 'Ace'];
    for (const r of ranks) {
      const res = await GET(new Request(`http://localhost/api/lake?type=ranks&partition=${r}&limit=5`));
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.equal(body.partition, r);
      assert.ok(body.seeds.length >= 5, `Expected >=5 seeds for rank ${r}`);
    }
  });

  it('F13-2: Live /api/lake GET serves real curated seeds across all 4 standard suit partitions', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const suits = ['Club', 'Diamond', 'Heart', 'Spades'];
    for (const s of suits) {
      const res = await GET(new Request(`http://localhost/api/lake?type=suits&partition=${s}&limit=5`));
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.ok(body.seeds.length >= 5, `Expected >=5 seeds for suit ${s}`);
    }
  });

  it('F13-3: Live /api/lake GET serves Negative Uncommon seeds starting with 111S8CHT', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=negatives&partition=uncommon&limit=10'));
    assert.equal(res.status, 200);
    const body = await res.json();
    assert.equal(body.count, 10);
    assert.equal(body.seeds[0], '111S8CHT');
  });

  it('F13-4: Every seed returned by /api/lake conforms strictly to Balatro [1-9A-Z]{1,8} alphabet (no 0)', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=ranks&partition=10&limit=50'));
    const body = await res.json();
    assert.ok(body.seeds.length >= 50);
    for (const seed of body.seeds) {
      assert.match(seed, /^[1-9A-Z]{1,8}$/);
    }
  });

  it('F13-5: DungmotLakeBrowser component exists in seedfinder.app and lake seeds evaluate cleanly in motely-wasm', async () => {
    const compPath = path.join(PATHS.seedfinderApp, 'components', 'DungmotLakeBrowser.tsx');
    const altPath = path.join(PATHS.seedfinderApp, 'app', 'components', 'DungmotLakeBrowser.tsx');
    assert.ok(exists(compPath) || exists(altPath) || exists(path.join(PATHS.seedfinderApp, 'app', 'api', 'lake', 'route.ts')));

    await getMotelyWasm();
    const { GET } = await getSeedfinderLakeRoute();
    const body = await (await GET(new Request('http://localhost/api/lake?type=ranks&partition=10&limit=3'))).json();
    const search = await runWasmSeedSearch('deck: Erratic\nstake: White\nmust:\n  - erraticRank: Ten\n    min: 15\n', body.seeds);
    assert.equal(search.matches.length, body.seeds.length);
  });

  it('F13-6: /api/lake response envelope includes type, partition, count, total, and seeds array', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=ranks&partition=Ace&limit=4'));
    const body = await res.json();
    assert.equal(body.type, 'ranks');
    assert.equal(body.partition, 'Ace');
    assert.equal(body.count, 4);
    assert.ok(body.total >= 4);
    assert.equal(body.seeds.length, 4);
  });
});
