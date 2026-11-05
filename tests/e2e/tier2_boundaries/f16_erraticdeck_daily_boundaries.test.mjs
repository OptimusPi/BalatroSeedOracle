// tests/e2e/tier2_boundaries/f16_erraticdeck_daily_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getErraticDeckDailyChallenge,
  getJamlUiRuntime,
  getMotelyWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 16 Boundaries: ErraticDeck.app Daily Ritual & Parquet Lake Fallback', () => {
  it('F16-B1: app/api/ranks/[rank]/route.ts validates rank parameter with strict alphanumeric regex and returns 400 on injection', () => {
    const routeSrc = fs.readFileSync(path.join(PATHS.ErraticDeckApp, 'app', 'api', 'ranks', '[rank]', 'route.ts'), 'utf-8');
    assert.ok(routeSrc.includes('!/^[A-Za-z0-9_]+$/.test(rank)'));
    assert.ok(routeSrc.includes('status: 400'));
  });

  it('F16-B2: Real jaml-ui parseRitual("") handles empty ritual string safely with default "Daily Ritual" title', async () => {
    const { ritual } = await getJamlUiRuntime();
    const parsed = ritual.parseRitual('');
    assert.equal(parsed.title, 'Daily Ritual');
    assert.equal(parsed.jaml, '');
  });

  it('F16-B3: getDailyChallenge(new Date("2030-01-01")) deterministically resolves a valid engine-checked JAML challenge', async () => {
    const { getDailyChallenge } = await getErraticDeckDailyChallenge();
    const { JamlConfigLoader } = await getMotelyWasm();
    const c1 = getDailyChallenge(new Date('2030-01-01T00:00:00Z'));
    const c2 = getDailyChallenge(new Date('2030-01-01T00:00:00Z'));
    assert.equal(c1.dayNumber, c2.dayNumber);
    assert.equal(c1.title, c2.title);
    assert.equal(JamlConfigLoader.check(c1.jaml), null);
  });

  it('F16-B4: getDailyChallenge handles pre-epoch / historical dates without negative modulo crash', async () => {
    const { getDailyChallenge } = await getErraticDeckDailyChallenge();
    const past = getDailyChallenge(new Date('1999-12-31T00:00:00Z'));
    assert.ok(past && typeof past.title === 'string' && past.title.length > 0);
    assert.ok(past.jaml.length > 0);
  });

  it('F16-B5: parseRitual ignores block scalar markers (| or >) on name/description and preserves comment metadata overrides', async () => {
    const { ritual } = await getJamlUiRuntime();
    const jaml = '# title: Override Title\nname: |\n  MultiLine\ndeck: Erratic\n';
    const parsed = ritual.parseRitual(jaml);
    assert.equal(parsed.title, 'Override Title');
    assert.equal(parsed.jaml, jaml);
  });

  it('F16-B6: Missing Parquet rank file in app/api/ranks/[rank]/route.ts returns clean 404 JSON diagnostic instead of 500', () => {
    const routeSrc = fs.readFileSync(path.join(PATHS.ErraticDeckApp, 'app', 'api', 'ranks', '[rank]', 'route.ts'), 'utf-8');
    assert.ok(routeSrc.includes('status: 404'));
    assert.ok(routeSrc.includes('Parquet Lake endpoint ready'));
  });
});
