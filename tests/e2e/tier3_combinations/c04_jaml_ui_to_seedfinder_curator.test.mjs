// tests/e2e/tier3_combinations/c04_jaml_ui_to_seedfinder_curator.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import { getMotelyWasm, getJamlUiRuntime } from '../helpers/runtime_oracle.mjs';

describe('Combination C04: jaml-ui SeedLab & JamlIde <-> seedfinder.app Curator Workspace', () => {
  it('C04-1: Built jaml-ui dist/index.js exports SeedLab, LiveJamlIde, and SeedHitTable', () => {
    const distIndex = path.join(PATHS.jamlUi, 'dist', 'index.js');
    assert.equal(exists(distIndex), true);
    const content = fs.readFileSync(distIndex, 'utf-8');
    assert.ok(content.includes('SeedLab'));
    assert.ok(content.includes('LiveJamlIde'));
    assert.ok(content.includes('SeedHitTable'));
  });

  it('C04-2: seedfinder.app Next.js production build output (.next/BUILD_ID) exists after compiling against jaml-ui', () => {
    const buildId = path.join(PATHS.seedfinderApp, '.next', 'BUILD_ID');
    assert.equal(exists(buildId), true, '.next/BUILD_ID must exist from clean Next.js build');
  });

  it('C04-3: Built SeedLab.js defines HIT_PAGE_SIZE, MAX_HITS, and SeedHitTable pagination', () => {
    const seedLabJs = path.join(PATHS.jamlUi, 'dist', 'components', 'SeedLab.js');
    assert.equal(exists(seedLabJs), true);
    const js = fs.readFileSync(seedLabJs, 'utf-8');
    assert.ok(js.includes('HIT_PAGE_SIZE') && js.includes('MAX_HITS'));
  });

  it('C04-4: DungmotLakeBrowser rank/suit JAML output loads cleanly in real motely-wasm JamlConfigLoader', async () => {
    const browserSrc = fs.readFileSync(
      path.join(PATHS.seedfinderApp, 'app', 'components', 'DungmotLakeBrowser.tsx'),
      'utf-8'
    );
    assert.ok(browserSrc.includes('erraticRank:') && browserSrc.includes('erraticSuit:'));

    const { JamlConfigLoader } = await getMotelyWasm();
    const rankYaml = `name: Dungmot Rank Ten\ndeck: Erratic\nstake: White\nseeds:\n  - "1UQ8D22L"\nshould:\n  - erraticRank: Ten\n`;
    const suitYaml = `name: Dungmot Suit Hearts\ndeck: Erratic\nstake: White\nseeds:\n  - "13TLL5FX"\nshould:\n  - erraticSuit: Hearts\n`;
    assert.equal(JamlConfigLoader.check(rankYaml), null);
    assert.equal(JamlConfigLoader.check(suitYaml), null);
  });

  it('C04-5: jaml-ui runtime tokens and vocab modules load cleanly in Node ESM', async () => {
    const { tokens, vocab } = await getJamlUiRuntime();
    assert.ok(tokens.palette && tokens.themes);
    assert.ok(Object.keys(vocab).length > 0);
  });
});
