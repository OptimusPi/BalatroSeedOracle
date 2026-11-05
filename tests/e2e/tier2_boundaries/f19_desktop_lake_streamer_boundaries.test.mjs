// tests/e2e/tier2_boundaries/f19_desktop_lake_streamer_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { runDesktopTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Feature 19 Boundaries: Desktop Dungmot Lake Streamer', () => {
  const sinkSrc = fs.readFileSync(
    path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'SeedLakeSink.cs'),
    'utf-8'
  );
  const vmSrc = fs.readFileSync(
    path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs'),
    'utf-8'
  );

  it('F19-B1: SeedLakeSink opens FileStream with FileShare.Read and AutoFlush = true for live concurrent readers', () => {
    assert.ok(sinkSrc.includes('FileShare.Read'));
    assert.ok(sinkSrc.includes('AutoFlush = true'));
  });

  it('F19-B2: SeedLakeSink serializes concurrent worker writes behind lock (_sync) and deduplicates via HashSet', () => {
    assert.ok(sinkSrc.includes('lock (_sync)'));
    assert.ok(sinkSrc.includes('_known.Add('));
  });

  it('F19-B3: DungmotLakeService handles missing lake directories cleanly in GpuAndLakeIntegrationTests', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~DungmotLakeService');
    assert.equal(res.exitCode, 0, `DungmotLakeService tests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F19-B4: Changing SelectedDbListIndex immediately updates SelectedLakePartition before search starts', () => {
    assert.ok(vmSrc.includes('partial void OnSelectedDbListIndexChanged'));
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_SelectedDbListIndexChanged_UpdatesSelectedLakePartition');
    assert.equal(res.exitCode, 0, `SelectedDbListIndex sync test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F19-B5: WordList mode loads built-in MotelyAesthetic dictionaries instead of falling through to Sequential mode', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_WordListMode_PopulatesAesthetics');
    assert.equal(res.exitCode, 0, `WordList aesthetic mode test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F19-B6: SeedLakeSink.SeedFilePath sanitizes path-separator characters (///) to safe fallback search.csv', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SeedLakeSinkTests');
    assert.equal(res.exitCode, 0, `SeedLakeSink filename sanitization test failed:\n${res.stdout}\n${res.stderr}`);
  });
});
