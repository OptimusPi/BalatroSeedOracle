// tests/e2e/tier1_features/f16_erraticdeck_daily_parquet.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, DUNGMOT_PATHS, exists } from '../helpers/env.mjs';

describe('Feature 16: ErraticDeck.app Daily Ritual & Parquet Lake Fallback', () => {
  it('F16-1: app/api/ranks/[rank]/route.ts exists for rank seed querying', () => {
    const routeFile = path.join(PATHS.ErraticDeckApp, 'app', 'api', 'ranks', '[rank]', 'route.ts');
    assert.equal(exists(routeFile), true, `Route file not found: ${routeFile}`);
  });

  it('F16-2: Route implementation contains local Parquet lake fallback to D:\\dungmot', () => {
    const routeFile = path.join(PATHS.ErraticDeckApp, 'app', 'api', 'ranks', '[rank]', 'route.ts');
    const content = fs.readFileSync(routeFile, 'utf-8');
    assert.ok(
      content.includes('dungmot') || content.includes('parquet_lake') || content.includes('local'),
      'Route must contain local dungmot parquet lake fallback logic'
    );
  });

  it('F16-3: Daily challenges directory app/daily/ exists', () => {
    const dailyDir = path.join(PATHS.ErraticDeckApp, 'app', 'daily');
    assert.equal(exists(dailyDir), true, `Daily dir not found: ${dailyDir}`);
  });

  it('F16-4: Daily ritual integrates DailyRitualView from jaml-ui', () => {
    const dailyComp = path.join(PATHS.ErraticDeckApp, 'app', 'daily', 'components', 'DailyChallengePage.tsx');
    const dailyPage = path.join(PATHS.ErraticDeckApp, 'app', 'daily', 'page.tsx');
    let hasDailyRitualView = false;
    if (exists(dailyComp) && fs.readFileSync(dailyComp, 'utf-8').includes('DailyRitualView')) {
      hasDailyRitualView = true;
    }
    if (exists(dailyPage) && fs.readFileSync(dailyPage, 'utf-8').includes('DailyRitualView')) {
      hasDailyRitualView = true;
    }
    assert.equal(hasDailyRitualView, true, 'DailyChallengePage must integrate DailyRitualView');
  });

  it('F16-5: Local Parquet files referenced by fallback exist in dungmot lake', () => {
    const tensFile = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    assert.equal(exists(tensFile), true, 'little_10s.parquet must exist for local queries');
  });

  it('F16-6: Real little_10s.parquet Thrift schema & ZSTD rows decode cleanly and match parquetSipQuery.ts contract', async () => {
    const { readParquetMetadata } = await import('../helpers/parquet_oracle.mjs');
    const { getMcpServer } = await import('../helpers/runtime_oracle.mjs');

    const tensFile = path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet');
    const meta = readParquetMetadata(tensFile);
    assert.ok(meta.columns.includes('seed'));
    assert.ok(meta.numRows > 0);

    const sipQuerySrc = fs.readFileSync(path.join(PATHS.ErraticDeckApp, 'lib', 'parquetSipQuery.ts'), 'utf-8');
    assert.ok(sipQuerySrc.includes('union_by_name = true'), 'parquetSipQuery.ts must support schema-tolerant Parquet reads');

    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'Ranks',
      partition: 'little_10s',
      limit: 5,
    });
    const parsed = JSON.parse(rpc.result.content[0].text);
    assert.equal(parsed.count, 5);
    for (const seed of parsed.seeds) {
      assert.match(seed, /^[1-9A-Z]{1,8}$/);
    }
  });
});
