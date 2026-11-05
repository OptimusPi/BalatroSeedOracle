// tests/e2e/tier3_combinations/c12_daily_ritual_jaml_to_results_view.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getJamlUiRuntime,
  getErraticDeckDailyChallenge,
  runWasmSeedSearch,
  analyzeSeedWithWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C12: jaml-ui parseRitual <-> ErraticDeck Daily Ritual & WASM Scoring', () => {
  const sampleRitualJaml = [
    '# title: Foil Friday Ritual',
    '# emoji: ✨',
    '# focusRank: 10',
    '# targetJoker: WalkieTalkie',
    '# theme: Foil Friday',
    'name: Foil Friday',
    'description: Score erratic tens and Walkie Talkie',
    'deck: Erratic',
    'stake: Gold',
    'must:',
    '  - erraticRank: Ten',
    '    min: 10',
    'should:',
    '  - erraticRank: Ten',
    '    score: 5',
  ].join('\n');

  it('C12-1: Real jaml-ui parseRitual extracts metadata comments and preserves engine-valid JAML verbatim', async () => {
    const { ritual } = await getJamlUiRuntime();
    const parsed = ritual.parseRitual(sampleRitualJaml);
    assert.equal(parsed.title, 'Foil Friday Ritual');
    assert.equal(parsed.description, 'Score erratic tens and Walkie Talkie');
    assert.equal(parsed.emoji, '✨');
    assert.equal(parsed.focusRank, '10');
    assert.equal(parsed.targetJoker, 'WalkieTalkie');
    assert.equal(parsed.theme, 'Foil Friday');
    assert.equal(parsed.jaml, sampleRitualJaml);
  });

  it('C12-2: Ritual JAML parsed by jaml-ui executes in motely-wasm and separates matching vs non-matching seeds', async () => {
    const { ritual } = await getJamlUiRuntime();
    const parsed = ritual.parseRitual(sampleRitualJaml);
    // 1UQ8D22L is top curated 10s seed; 11111111 does not have >=10 tens
    const res = await runWasmSeedSearch(parsed.jaml, ['1UQ8D22L', '11111111']);
    assert.equal(res.totalSeedsSearched, 2);
    assert.ok(res.matches.includes('1UQ8D22L'), 'Expected curated 10s seed 1UQ8D22L to match ritual');
    assert.ok(!res.matches.includes('11111111'), 'Expected control seed 11111111 not to match >=10 tens');
  });

  it('C12-3: Scored WASM search produces positive numeric score tally for matching ritual seeds', async () => {
    const res = await runWasmSeedSearch(sampleRitualJaml, ['1UQ8D22L', '1V3UQ6AP', '11111111']);
    const hit = res.scored.find(s => s.seed === '1UQ8D22L');
    assert.ok(hit, 'Expected scored result for 1UQ8D22L');
    assert.ok(hit.score >= 50, `Expected score >= 50 (5 pts * >=10 tens), got ${hit.score}`);
  });

  it('C12-4: Daily ritual seed hit feeds directly into MotelyJamlyzer WASM inspection with 52-card Erratic deck', async () => {
    const jam = await analyzeSeedWithWasm('1UQ8D22L', sampleRitualJaml);
    assert.ok(!jam.error, `Unexpected Jamlyzer error: ${jam.error}`);
    assert.ok(Array.isArray(jam.erraticDeck));
    assert.equal(jam.erraticDeck.length, 52);
  });

  it('C12-5: Real ErraticDeck.app getDailyChallenge JAML parses cleanly via jaml-ui parseRitual', async () => {
    const { ritual } = await getJamlUiRuntime();
    const { getDailyChallenge } = await getErraticDeckDailyChallenge();
    const challenge = getDailyChallenge(new Date('2026-11-04T12:00:00Z'));
    const parsed = ritual.parseRitual(challenge.jaml);
    assert.ok(parsed.title.length > 0);
    assert.equal(parsed.jaml, challenge.jaml);
  });
});
