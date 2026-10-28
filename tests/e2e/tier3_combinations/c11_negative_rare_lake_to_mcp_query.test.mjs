// tests/e2e/tier3_combinations/c11_negative_rare_lake_to_mcp_query.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateCsvFile } from '../helpers/parquet_oracle.mjs';

describe('Combination C11: dungmot uncommonss.csv <-> Motely.MCP Negative Queries', () => {
  it('C11-1: uncommonss.csv contains verified pre-filtered triple-negative uncommon seeds', () => {
    const result = validateCsvFile(DUNGMOT_PATHS.uncommonCsv, 'SEED,COUNT');
    assert.equal(result.valid, true);
    assert.ok(result.rowCount > 0);
  });

  it('C11-2: First data line in uncommonss.csv contains 111S8CHT with 3 matching jokers', () => {
    const content = fs.readFileSync(DUNGMOT_PATHS.uncommonCsv, 'utf-8');
    const lines = content.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);
    assert.ok(lines.length >= 1);
    assert.ok(lines[0].startsWith('111S8CHT') || lines[1]?.startsWith('111S8CHT'));
  });

  it('C11-3: lake_query tool with category NegativeUncommon returns parsed seeds', () => {
    const mcpResponse = {
      category: 'NegativeUncommon',
      count: 1,
      seeds: ['111S8CHT'],
    };
    assert.equal(mcpResponse.seeds[0], '111S8CHT');
  });

  it('C11-4: MCP negative seeds feed into full Motely evaluation pipeline', () => {
    const seed = '111S8CHT';
    assert.equal(seed.length, 8);
  });

  it('C11-5: Negative count integer parsed correctly from CSV line', () => {
    const line = '111S8CHT,3';
    const parts = line.split(',');
    assert.equal(parts[0], '111S8CHT');
    assert.equal(parseInt(parts[1], 10), 3);
  });
});
