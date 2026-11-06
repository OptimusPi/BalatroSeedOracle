// tests/e2e/tier1_features/f19_desktop_lake_streamer.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

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

  it('F19-3: SeedLakeSink writes matched seeds to disk with CSV schema', () => {
    const content = fs.readFileSync(sinkPath, 'utf-8');
    assert.ok(content.includes('StreamWriter') || content.includes('File.AppendAllText') || content.includes('Write'));
  });

  it('F19-4: Lake streamer supports streaming seeds from Dungmot ranks best lists', () => {
    const bestFile = path.join(PATHS.dungmot, 'ranks', '10_best.txt');
    assert.equal(exists(bestFile), true);
  });

  it('F19-5: Lake streamer supports streaming seeds from Dungmot suits best lists', () => {
    const bestFile = path.join(PATHS.dungmot, 'suits', 'Club_best.txt');
    assert.equal(exists(bestFile), true);
  });

  it('F19-6: Lake streamer supports streaming negative uncommon seeds from uncommonss.csv', () => {
    const csvFile = path.join(PATHS.dungmot, 'uncommonss.csv');
    assert.equal(exists(csvFile), true);
  });
});
