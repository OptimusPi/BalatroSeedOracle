// tests/e2e/tier1_features/f04_seed_lake_service.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateParquetFile, validateSeedListFile, validateCsvFile } from '../helpers/parquet_oracle.mjs';

describe('Feature 4: Seed Lake Service in Motely (ISeedLakeService)', () => {
  it('F04-1: Curated rank seeds list ranks/10_best.txt exists and contains valid seeds', () => {
    const file = path.join(DUNGMOT_PATHS.ranksDir, '10_best.txt');
    const result = validateSeedListFile(file, 50);
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.seedCount >= 50, `Found ${result.seedCount} seeds`);
  });

  it('F04-2: Curated suit seeds list suits/Club_best.txt exists and contains valid seeds', () => {
    const file = path.join(DUNGMOT_PATHS.suitsDir, 'Club_best.txt');
    const result = validateSeedListFile(file, 50);
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.seedCount >= 50, `Found ${result.seedCount} seeds`);
  });

  it('F04-3: Negative uncommon seeds catalog uncommonss.csv exists and has valid SEED,COUNT schema', () => {
    const file = DUNGMOT_PATHS.uncommonCsv;
    const result = validateCsvFile(file, 'SEED,COUNT');
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.rowCount >= 100, `Expected >= 100 rows, got ${result.rowCount}`);
  });

  it('F04-4: Parquet lake rank file has valid PAR1 magic header and footer', () => {
    const file = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    const result = validateParquetFile(file);
    assert.equal(result.valid, true, result.reason);
  });

  it('F04-5: Parquet lake suit file has valid PAR1 magic header and footer', () => {
    const file = path.join(DUNGMOT_PATHS.parquetSuits, 'little_Clubs.parquet');
    const result = validateParquetFile(file);
    assert.equal(result.valid, true, result.reason);
  });

  it('F04-6: LakeCategory enumeration in ISeedLakeService.cs maps all 7 categories and passes SeedLakeTests', async () => {
    const fs = await import('node:fs');
    const { PATHS } = await import('../helpers/env.mjs');
    const { runMotelyTestsFilter } = await import('../helpers/runtime_oracle.mjs');
    const lakeInterface = path.join(PATHS.MotelyJAML, 'Motely', 'Lake', 'ISeedLakeService.cs');
    const src = fs.readFileSync(lakeInterface, 'utf-8');
    const categories = [
      'Ranks',
      'Suits',
      'CuratedRanks',
      'CuratedSuits',
      'NegativeUncommon',
      'NegativeRare',
      'TripleNegative',
    ];
    for (const cat of categories) {
      assert.ok(src.includes(cat), `ISeedLakeService.cs must declare LakeCategory.${cat}`);
    }
    const res = runMotelyTestsFilter('FullyQualifiedName~SeedLakeTests');
    assert.equal(res.exitCode, 0, `SeedLakeTests failed:\n${res.stdout}\n${res.stderr}`);
  });
});
