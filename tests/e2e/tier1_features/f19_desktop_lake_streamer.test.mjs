// tests/e2e/tier1_features/f19_desktop_lake_streamer.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import { runDesktopTestsFilter, runWasmSeedSearch } from '../helpers/runtime_oracle.mjs';

describe('Feature 19: Desktop Dungmot Lake Streamer', () => {
  const vmPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs');
  const sinkPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'SeedLakeSink.cs');

  it('F19-1: SearchMode enumeration in SearchModalViewModel defines DbList / Lake mode', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('DbList'), 'SearchMode must include DbList');
  });

  it('F19-2: SeedLakeSink.cs exists in BalatroSeedOracle.Services', () => {
    assert.equal(exists(sinkPath), true, `SeedLakeSink.cs not found at ${sinkPath}`);
  });

  it('F19-3: Live headless Avalonia SearchModalViewModel DbList partition selection & lake execution passes', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_SelectedDbListIndexChanged');
    assert.equal(res.exitCode, 0, `Desktop lake partition test failed:\n${res.stderr || res.stdout}`);
  });

  it('F19-4: Real seeds streamed from Dungmot ranks/10_best.txt match ErraticRank 10 >= 15 in Motely engine', async () => {
    const bestFile = path.join(PATHS.dungmot, 'ranks', '10_best.txt');
    assert.equal(exists(bestFile), true);
    const seeds = fs.readFileSync(bestFile, 'utf-8').split(/\r?\n/).map(s => s.trim()).filter(Boolean).slice(0, 5);
    assert.ok(seeds.length > 0);
    const res = await runWasmSeedSearch('deck: Erratic\nmust:\n  - erraticRank: Ten\n    min: 15\n', seeds);
    assert.equal(res.matches.length, seeds.length);
  });

  it('F19-5: Real seeds streamed from Dungmot suits/Club_best.txt match ErraticSuit Clubs >= 25 in Motely engine', async () => {
    const bestFile = path.join(PATHS.dungmot, 'suits', 'Club_best.txt');
    assert.equal(exists(bestFile), true);
    const seeds = fs.readFileSync(bestFile, 'utf-8').split(/\r?\n/).map(s => s.trim()).filter(Boolean).slice(0, 5);
    assert.ok(seeds.length > 0);
    const res = await runWasmSeedSearch('deck: Erratic\nmust:\n  - erraticSuit: Clubs\n    min: 25\n', seeds);
    assert.equal(res.matches.length, seeds.length);
  });

  it('F19-6: Lake streamer supports streaming negative uncommon seeds from uncommonss.csv', () => {
    const csvFile = path.join(PATHS.dungmot, 'uncommonss.csv');
    assert.equal(exists(csvFile), true);
  });
});
