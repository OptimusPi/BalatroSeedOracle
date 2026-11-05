// tests/e2e/tier1_features/f10_jaml_ui_high_level_reexports.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 10: jaml-ui High-Level & Core UI Runtime Re-Exports (dist/index.js)', async () => {
  const distIndexUrl = pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'index.js')).href;
  const mod = await import(distIndexUrl);

  it('F10-1: Runtime exports SeedLab, LiveJamlIde, and STARTER_JAML', () => {
    assert.equal(typeof mod.SeedLab, 'function');
    assert.equal(typeof mod.LiveJamlIde, 'function');
    assert.equal(typeof mod.STARTER_JAML, 'string');
  });

  it('F10-2: Runtime exports JamlIde, ResultsView, and TallyBar', () => {
    assert.equal(typeof mod.JamlIde, 'function');
    assert.equal(typeof mod.ResultsView, 'function');
    assert.equal(typeof mod.TallyBar, 'function');
  });

  it('F10-3: Runtime exports Jamlyzer, JamlyzerView, and JamlyzerBulk', () => {
    assert.equal(typeof mod.Jamlyzer, 'function');
    assert.equal(typeof mod.JamlyzerView, 'function');
    assert.equal(typeof mod.JamlyzerBulk, 'function');
  });

  it('F10-4: Runtime exports DailyRitualView and parseRitual helper', () => {
    assert.equal(typeof mod.DailyRitualView, 'function');
    assert.equal(typeof mod.parseRitual, 'function');
  });

  it('F10-5: Runtime exports DeckStakePicker and deck/stake constant arrays', () => {
    assert.equal(typeof mod.DeckStakePicker, 'function');
    assert.ok(Array.isArray(mod.DECK_PICKER_NAMES) && mod.DECK_PICKER_NAMES.length === 15);
    assert.ok(Array.isArray(mod.STAKE_PICKER_NAMES) && mod.STAKE_PICKER_NAMES.length === 8);
  });

  it('F10-6: Runtime exports JimboBalatroFooter, JamlGameCard / GameCard, and all Motely.Web UI primitives', () => {
    for (const sym of [
      'JimboBalatroFooter',
      'JamlGameCard',
      'GameCard',
      'JimboApp',
      'JimboBackground',
      'JimboBadge',
      'JimboButton',
      'JimboErrorBlock',
      'JimboPanel',
      'JimboRow',
      'JimboSectionHeader',
      'JimboStack',
      'JimboStatusPill',
      'JimboTabs',
      'JimboText',
      'JimboTextInput',
      'JimboWordmark',
    ]) {
      assert.equal(typeof mod[sym], 'function', `dist/index.js must export function ${sym}`);
    }
  });
});

