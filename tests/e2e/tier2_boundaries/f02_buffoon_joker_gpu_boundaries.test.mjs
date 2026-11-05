// tests/e2e/tier2_boundaries/f02_buffoon_joker_gpu_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  iteratePrng,
} from '../helpers/prng_oracle.mjs';
import {
  getMotelyWasm,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 2 Boundaries: Buffoon Pack GPU Prefilter', () => {
  it('F02-B1: min: 0 on booster-pack joker clause acts as trivial pass for any valid seed', async () => {
    const res = await runWasmSeedSearch(
      'deck: Red\nstake: White\nmust:\n  - uncommonJoker: []\n    min: 0\n    antes: [1]\n    sources:\n      boosterPacks: [0, 1]\n',
      ['ALEEB', '111S8CHT']
    );
    assert.equal(res.matches.length, 2);
  });

  it('F02-B2: iteratePrng produces strictly bounded [0, 1) rolls across 10 consecutive draws', () => {
    let state = 0.54321;
    for (let i = 0; i < 10; i++) {
      state = iteratePrng(state);
      assert.ok(state >= 0.0 && state < 1.0, `Roll at step ${i} out of bounds: ${state}`);
    }
  });

  it('F02-B3: Ante 16 boundary key generation matches edibuf16 and rarity16buf', () => {
    assert.equal(getBuffoonEditionKey(16), 'edibuf16');
    assert.equal(getBuffoonRarityKey(16), 'rarity16buf');
  });

  it('F02-B4: Impossible min count (min: 20 in single ante booster pack) cleanly produces 0 matches', async () => {
    const res = await runWasmSeedSearch(
      'deck: Red\nstake: White\nmust:\n  - uncommonJoker: []\n    min: 20\n    antes: [1]\n    sources:\n      boosterPacks: [0]\n',
      ['ALEEB', '111S8CHT', '1UQ8D22L']
    );
    assert.equal(res.matches.length, 0);
  });

  it('F02-B5: Omitted edition constraint accepts all MotelyItemEdition variants in WASM config', async () => {
    const { JamlConfigLoader, MotelyItemEdition } = await getMotelyWasm();
    for (const ed of ['None', 'Foil', 'Holographic', 'Polychrome', 'Negative']) {
      assert.notEqual(MotelyItemEdition[ed], undefined, `Missing MotelyItemEdition.${ed}`);
    }
    assert.equal(JamlConfigLoader.check('deck: Red\nstake: White\nmust:\n  - commonJoker: []\n    antes: [1]\n'), null);
  });

  it('F02-B6: All three rarity-wildcard clauses (commonJoker, uncommonJoker, rareJoker) compile cleanly in WASM', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    for (const key of ['commonJoker', 'uncommonJoker', 'rareJoker']) {
      const err = JamlConfigLoader.check(`deck: Red\nstake: White\nmust:\n  - ${key}: []\n    antes: [1]\n`);
      assert.equal(err, null, `Unexpected JAML check error for ${key}: ${err}`);
    }
  });
});
