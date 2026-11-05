// tests/e2e/tier4_scenarios/s01_negative_uncommon_hunt_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateCsvFile } from '../helpers/parquet_oracle.mjs';
import {
  getMotelyWasm,
  runWasmSeedSearch,
  analyzeSeedWithWasm,
  getMcpServer,
} from '../helpers/runtime_oracle.mjs';

describe('Tier 4 Scenario 1: Negative Uncommon Joker Hunt Workflow', () => {
  const jamlFilter = `name: Negative Uncommon Hunt
deck: Red
stake: White
should:
  - uncommonJoker: []
    edition: Negative
    antes: [1, 2, 3, 4, 5, 6, 7, 8]
    score: 1
`;

  it('Step 1: User queries dungmot negative lake catalog via CSV oracle and live Motely.MCP lake_query', async () => {
    assert.equal(exists(DUNGMOT_PATHS.uncommonCsv), true);
    const csvCheck = validateCsvFile(DUNGMOT_PATHS.uncommonCsv, 'SEED,COUNT');
    assert.equal(csvCheck.valid, true, csvCheck.reason);
    assert.ok(csvCheck.rowCount >= 100);

    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('lake_query', {
      category: 'NegativeUncommon',
      partition: 'uncommonss',
      limit: 5,
    });
    const payload = JSON.parse(rpc.result.content[0].text);
    assert.equal(payload.count, 5);
    assert.ok(payload.seeds.includes('111S8CHT'));
  });

  it('Step 2: User extracts top candidate seed 111S8CHT and count from uncommonss.csv', () => {
    const content = fs.readFileSync(DUNGMOT_PATHS.uncommonCsv, 'utf-8');
    const lines = content.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);
    const dataLine = lines.find(l => l.startsWith('111S8CHT,'));
    assert.ok(dataLine, '111S8CHT row must exist in uncommonss.csv');
    const [seed, countStr] = dataLine.split(',');
    assert.equal(seed, '111S8CHT');
    assert.ok(Number(countStr) >= 3, `Expected >= 3 Negative Uncommon jokers in CSV, got ${countStr}`);
  });

  it('Step 3: JamlConfigLoader parses and validates Negative Uncommon JAML filter in motely-wasm', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    assert.equal(JamlConfigLoader.check(jamlFilter), null);
    const config = JamlConfigLoader.fromJaml(jamlFilter);
    assert.equal(config.should.length, 1);
  });

  it('Step 4: Live GPU/SIMD engine search evaluates candidate 111S8CHT vs control seed AAAAAAAA', async () => {
    const res = await runWasmSeedSearch(jamlFilter, ['111S8CHT', 'AAAAAAAA']);
    assert.equal(res.totalSeedsSearched, 2);
    const hit = res.scored.find(r => r.seed === '111S8CHT');
    const ctrl = res.scored.find(r => r.seed === 'AAAAAAAA');
    assert.ok(hit, '111S8CHT must be scored by engine');
    assert.ok(hit.score > (ctrl?.score ?? 0), `Expected 111S8CHT score (${hit.score}) > AAAAAAAA (${ctrl?.score ?? 0})`);
  });

  it('Step 5: MotelyJamlyzer confirms real Negative shop/pack items on 111S8CHT across Antes 1-8', async () => {
    const { MotelyItemEdition } = await getMotelyWasm();
    const analysis = await analyzeSeedWithWasm('111S8CHT', jamlFilter);
    assert.equal(analysis.seed, '111S8CHT');
    assert.ok(analysis.antes.length >= 8);

    let negativeStreamHits = 0;
    for (const ante of analysis.antes) {
      for (const item of ante.shopStreams.uncommonShopJokers) {
        if (item.edition === MotelyItemEdition.Negative) negativeStreamHits++;
      }
      for (const item of ante.shopItems) {
        if (item.edition === MotelyItemEdition.Negative) negativeStreamHits++;
      }
    }
    assert.ok(analysis.score >= 1 || negativeStreamHits >= 1, 'Jamlyzer must surface Negative Uncommon hits on 111S8CHT');
  });
});
