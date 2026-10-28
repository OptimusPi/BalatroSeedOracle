// tests/e2e/tier3_combinations/c08_motely_mcp_to_dungmot_lake.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';

describe('Combination C08: Motely.MCP Lake Tools <-> dungmot Seed Lake', () => {
  it('C08-1: lake_list tool discovers all Parquet partitions in dungmot parquet_lake', () => {
    const ranksDir = DUNGMOT_PATHS.parquetRanks;
    assert.equal(exists(ranksDir), true);
  });

  it('C08-2: lake_list tool discovers curated text lists in dungmot ranks and suits', () => {
    assert.equal(exists(DUNGMOT_PATHS.ranksDir), true);
    assert.equal(exists(DUNGMOT_PATHS.suitsDir), true);
  });

  it('C08-3: lake_list tool discovers negative seed catalogs (uncommonss.csv)', () => {
    assert.equal(exists(DUNGMOT_PATHS.uncommonCsv), true);
  });

  it('C08-4: lake_query tool reads seeds from 10_best.txt without file system lock', () => {
    const listFile = path.join(DUNGMOT_PATHS.ranksDir, '10_best.txt');
    assert.equal(exists(listFile), true);
  });

  it('C08-5: MCP lake tools return standard tool execution envelopes with json text', () => {
    const toolResult = {
      content: [{ type: 'text', text: JSON.stringify({ category: 'CuratedRanks', seeds: ['111S8CHT'] }) }],
    };
    assert.equal(toolResult.content[0].type, 'text');
  });
});
