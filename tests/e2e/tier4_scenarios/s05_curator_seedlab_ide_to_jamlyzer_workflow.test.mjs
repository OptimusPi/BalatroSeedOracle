// tests/e2e/tier4_scenarios/s05_curator_seedlab_ide_to_jamlyzer_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getMotelyWasm,
  analyzeSeedWithWasm,
  getJamlUiRuntime,
} from '../helpers/runtime_oracle.mjs';

describe('Tier 4 Scenario 5: Curator SeedLab IDE to Jamlyzer Workflow', () => {
  const curatorJaml = `name: Erratic Gold Rare Tag
deck: Erratic
stake: Gold
must:
  - smallBlindTag: RareTag
    antes: [1]
`;

  let matchedSeed = '';

  it('Step 1: seedfinder.app production build and jaml-ui runtime tokens are ready', async () => {
    assert.equal(exists(path.join(PATHS.seedfinderApp, '.next')), true);
    const { tokens } = await getJamlUiRuntime();
    assert.ok(Array.isArray(tokens.toneNames) && tokens.toneNames.length > 0);
  });

  it('Step 2: Curator JAML filter (Deck=Erratic, Stake=Gold, SmallBlindTag=RareTag) compiles in motely-wasm', async () => {
    const { JamlConfigLoader, MotelyDeck, MotelyStake } = await getMotelyWasm();
    assert.equal(JamlConfigLoader.check(curatorJaml), null);
    const cfg = JamlConfigLoader.fromJaml(curatorJaml);
    assert.equal(cfg.deck, MotelyDeck.Erratic);
    assert.equal(cfg.stake, MotelyStake.Gold);
    assert.equal(cfg.must.length, 1);
  });

  it('Step 3: In-process motely-wasm search sweeps batch 0 and discovers real matching seeds', async () => {
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    const cfg = JamlConfigLoader.fromJaml(curatorJaml);
    const hits = [];
    const search = JamlSearchBuilder.createSettings(cfg)
      .withThreadCount(1)
      .withQuietMode(true)
      .withSequentialSearch()
      .withBatchCharacterCount(3)
      .withEndBatchIndex(1n)
      .withSeedMatchCallback(s => hits.push(s))
      .start();
    try {
      await search.waitForCompletionAsync();
    } finally {
      search.dispose();
    }
    assert.ok(hits.length > 0, 'Expected at least one RareTag Ante 1 seed in batch 0');
    matchedSeed = hits[0];
    assert.match(matchedSeed, /^[1-9A-Z]{8}$/);
  });

  it('Step 4: Jamlyzer inspects top hit and confirms 52-card Erratic Deck + Ante 1 RareTag', async () => {
    const { MotelyTag } = await getMotelyWasm();
    const result = await analyzeSeedWithWasm(matchedSeed, curatorJaml);
    assert.equal(result.seed, matchedSeed);
    assert.ok(result.erraticDeck);
    assert.equal(result.erraticDeck.length, 52);

    const ante1 = result.antes.find(a => a.ante === 1);
    assert.ok(ante1, 'Ante 1 breakdown must exist');
    assert.equal(ante1.smallBlindTag, MotelyTag.RareTag);
  });

  it('Step 5: JamlConfigLoader round-trips YAML -> JSON -> YAML preserving seed strings cleanly', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const seededYaml = `${curatorJaml}seeds:\n  - "${matchedSeed}"\n`;
    const json = JamlConfigLoader.toJson(seededYaml);
    const parsedJson = JSON.parse(json);
    assert.deepEqual(parsedJson.seeds, [matchedSeed]);
    const yamlAgain = JamlConfigLoader.toYaml(json);
    const roundTripped = JamlConfigLoader.fromJaml(yamlAgain);
    assert.deepEqual([...roundTripped.seeds], [matchedSeed]);
  });
});
