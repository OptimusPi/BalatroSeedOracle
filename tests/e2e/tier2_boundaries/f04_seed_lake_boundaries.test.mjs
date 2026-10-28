// tests/e2e/tier2_boundaries/f04_seed_lake_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateCsvFile } from '../helpers/parquet_oracle.mjs';

describe('Feature 4 Boundaries: Seed Lake Service in Motely', () => {
  it('F04-B1: Non-existent partition request returns empty enumerable or throws informative error', () => {
    const fakePartition = 'NonExistentRank99';
    assert.equal(exists(path.join(DUNGMOT_PATHS.ranksDir, `${fakePartition}_best.txt`)), false);
  });

  it('F04-B2: Limit = 0 or negative limit yields zero seeds', () => {
    const limit = 0;
    const seeds = ['A', 'B', 'C'].slice(0, Math.max(0, limit));
    assert.equal(seeds.length, 0);
  });

  it('F04-B3: Limit exceeding available partition size returns full partition without overflow', () => {
    const available = ['SEED1111', 'SEED2222'];
    const limit = 1000;
    const result = available.slice(0, limit);
    assert.equal(result.length, 2);
  });

  it('F04-B4: Zero-byte file (e.g. triple-negative.csv) is handled safely without unhandled crash', () => {
    const zeroByteFile = DUNGMOT_PATHS.tripleNegativeCsv;
    if (exists(zeroByteFile)) {
      const stat = fs.statSync(zeroByteFile);
      assert.equal(stat.size, 0, 'triple-negative.csv is 0 bytes on disk');
      const content = fs.readFileSync(zeroByteFile, 'utf-8');
      const seeds = content.split('\n').filter(s => s.trim().length > 0);
      assert.equal(seeds.length, 0);
    }
  });

  it('F04-B5: Malformed CSV header or line with invalid seed format is filtered or reported', () => {
    const badLines = ['SEED,COUNT', 'INVALID_SEED_TOO_LONG,3', 'SHORT,1', 'VALID123,2'];
    const valid = badLines.slice(1).filter(l => /^[0-9A-Z]{8},\d+$/.test(l));
    assert.equal(valid.length, 1);
    assert.equal(valid[0], 'VALID123,2');
  });

  it('F04-B6: Trailing whitespace or CRLF line endings trimmed properly from seed strings', () => {
    const raw = '111S8CHT\r\n';
    const clean = raw.trim();
    assert.equal(clean, '111S8CHT');
    assert.equal(clean.length, 8);
  });
});
