// tests/e2e/tier3_combinations/c11_negative_rare_lake_to_mcp_query.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateCsvFile } from '../helpers/parquet_oracle.mjs';
import { getMcpServer, analyzeSeedWithWasm } from '../helpers/runtime_oracle.mjs';

describe('Combination C11: dungmot uncommonss.csv / negrares.csv <-> Motely.MCP Negative Queries', () => {
  it('C11-1: uncommonss.csv contains valid SEED,COUNT rows while negrares.csv exists on disk', () => {
    const unc = validateCsvFile(DUNGMOT_PATHS.uncommonCsv, 'SEED,COUNT');
    assert.equal(unc.valid, true, unc.reason);
    assert.ok(unc.rowCount > 0);
    assert.equal(exists(DUNGMOT_PATHS.negraresCsv), true);
  });

  it('C11-2: First data row in uncommonss.csv starts with 111S8CHT and integer count >= 3', () => {
    const content = fs.readFileSync(DUNGMOT_PATHS.uncommonCsv, 'utf-8');
    const lines = content.split(/\r?\n/).map(l => l.trim()).filter(Boolean);
    const dataLine = lines.find(l => !l.toUpperCase().startsWith('SEED'));
    assert.ok(dataLine, 'Expected at least one data row in uncommonss.csv');
    const [seed, countRaw] = dataLine.split(',');
    assert.equal(seed, '111S8CHT');
    assert.ok(Number.parseInt(countRaw, 10) >= 3);
  });

  it('C11-3: Live Motely.MCP lake_query with category NegativeUncommon returns real parsed seeds without count suffix', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'NegativeUncommon',
      partition: 'uncommonss',
      limit: 5,
    });
    const body = JSON.parse(rpc.result.content[0].text);
    assert.equal(body.count, 5);
    assert.equal(body.seeds[0], '111S8CHT');
    for (const s of body.seeds) {
      assert.match(s, /^[1-9A-Z]{1,8}$/);
    }
  });

  it('C11-4: Live Motely.MCP lake_query on NegativeRare safely ignores non-seed Progress log lines without leaking invalid tokens', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'NegativeRare',
      partition: 'negrares',
      limit: 5,
    });
    const body = JSON.parse(rpc.result.content[0].text);
    assert.ok(Array.isArray(body.seeds));
    for (const s of body.seeds) {
      assert.match(s, /^[1-9A-Z]{1,8}$/, `Non-seed progress line leaked into seeds: '${s}'`);
    }
  });

  it('C11-5: MCP negative seed 111S8CHT feeds directly into real MotelyJamlyzer WASM analysis with 0 errors', async () => {
    const res = await analyzeSeedWithWasm('111S8CHT', { deck: 'Red', stake: 'White' });
    assert.ok(!res.error, `Unexpected Jamlyzer error: ${res.error}`);
    assert.equal(res.seed, '111S8CHT');
    assert.ok(res.antes.length >= 1);
  });
});
