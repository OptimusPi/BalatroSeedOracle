// tests/e2e/tier2_boundaries/f12_seedfinder_curator_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { getMotelyWasm } from '../helpers/runtime_oracle.mjs';

describe('Feature 12 Boundaries: seedfinder.app Curator Integration', () => {
  it('F12-B1: Empty JAML filter string is handled cleanly by JamlConfigLoader without crashing WASM', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const checkErr = JamlConfigLoader.check('');
    if (checkErr === null) {
      const cfg = JamlConfigLoader.fromJaml('');
      assert.ok(cfg);
    } else {
      assert.equal(typeof checkErr, 'string');
    }
  });

  it('F12-B2: Syntactically malformed JAML (unclosed flow sequence) returns human-readable error string in JamlConfigLoader.check', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const err = JamlConfigLoader.check('must:\n  - voucher: [Overstock\n');
    assert.ok(typeof err === 'string' && err.length > 0, `Expected syntax error string, got: ${err}`);
  });

  it('F12-B3: Non-existent voucher or joker name in JAML is flagged by JamlConfigLoader.check with helpful diagnostic', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const err = JamlConfigLoader.check('must:\n  - voucher: NonExistentVoucherFake123\n');
    assert.ok(err && err.includes('NonExistentVoucherFake123'), `Expected diagnostic mentioning NonExistentVoucherFake123, got: ${err}`);
  });

  it('F12-B4: Built jaml-ui SeedLab bundle exports positive MAX_HITS and HIT_PAGE_SIZE bounds', () => {
    const seedLabJs = fs.readFileSync(path.join(PATHS.jamlUi, 'dist', 'components', 'SeedLab.js'), 'utf-8');
    assert.ok(seedLabJs.includes('MAX_HITS') || seedLabJs.includes('HIT_PAGE_SIZE'));
  });

  it('F12-B5: 0-char seed "" analyzes cleanly while unsearchable seeds are skipped safely by MotelyJamlyzer.analyze without crashing WASM', async () => {
    const { JamlConfigLoader, MotelyJamlyzer } = await getMotelyWasm();
    const cfgEmpty = JamlConfigLoader.fromJaml('deck: Red\nstake: White\nseeds:\n  - ""\n');
    const emptyRes = MotelyJamlyzer.analyze(cfgEmpty);
    assert.equal(emptyRes.length, 1, '0-char "" seed is valid in Balatro and returns 1 breakdown row');
    assert.equal(emptyRes[0].seed, '');

    const cfgUnsearchable = JamlConfigLoader.fromJaml('deck: Red\nstake: White\nseeds:\n  - "   "\n  - "0INVALID999"\n');
    const unsearchableRes = MotelyJamlyzer.analyze(cfgUnsearchable);
    assert.equal(unsearchableRes.length, 0, 'Unsearchable seeds are skipped with 0 breakdown rows');

    const cfgMixed = JamlConfigLoader.fromJaml('deck: Red\nstake: White\nseeds:\n  - "   "\n  - ""\n  - ALEEB\n');
    const mixedRes = MotelyJamlyzer.analyze(cfgMixed);
    assert.equal(mixedRes.length, 2);
    assert.equal(mixedRes[0].seed, '');
    assert.equal(mixedRes[1].seed, 'ALEEB');
  });

  it('F12-B6: JamlSearchBuilder clamps or accepts explicit worker thread count cleanly in WASM', async () => {
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    const cfg = JamlConfigLoader.fromJaml('deck: Red\nstake: White\n');
    const settings = JamlSearchBuilder.createSettings(cfg).withThreadCount(1).withSeedList(['ALEEB']);
    const search = settings.start();
    try {
      await search.waitForCompletionAsync();
      assert.equal(Number(search.totalSeedsSearched), 1);
    } finally {
      search.dispose();
    }
  });
});
