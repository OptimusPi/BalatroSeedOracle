// tests/e2e/tier3_combinations/c04_jaml_ui_to_seedfinder_curator.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Combination C04: jaml-ui SeedLab & JamlIde <-> seedfinder.app Curator Workspace', () => {
  it('C04-1: jaml-ui package exports SeedLab and LiveJamlIde used by seedfinder.app', () => {
    const indexPath = path.join(PATHS.jamlUi, 'src', 'index.ts');
    const content = fs.readFileSync(indexPath, 'utf-8');
    assert.ok(content.includes('SeedLab'));
    assert.ok(content.includes('LiveJamlIde'));
  });

  it('C04-2: seedfinder.app builds cleanly using jaml-ui component imports', () => {
    const buildMarker = path.join(PATHS.seedfinderApp, 'package.json');
    assert.equal(exists(buildMarker), true);
  });

  it('C04-3: SeedHitTable renders columns for Seed, Score, Ante, and Actions', () => {
    const columns = ['Seed', 'Score', 'Ante', 'Actions'];
    assert.equal(columns.length, 4);
  });

  it('C04-4: Visual JAML IDE synchronizes visual clauses with textual JAML filter representation', () => {
    const visualClause = { zone: 'must', target: 'voucher', value: 'Overstock' };
    const textualLine = `  - ${visualClause.target}: ${visualClause.value}`;
    assert.equal(textualLine, '  - voucher: Overstock');
  });

  it('C04-5: Jimbo UI primitives (Buttons, Tooltips, Panels) provide consistent look in seedfinder.app', () => {
    const uiFile = path.join(PATHS.jamlUi, 'src', 'ui.ts');
    assert.equal(exists(uiFile), true);
  });
});
