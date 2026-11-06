// tests/e2e/tier1_features/f10_jaml_ui_high_level_reexports.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 10: jaml-ui High-Level Re-Exports (src/index.ts)', () => {
  const indexPath = path.join(PATHS.jamlUi, 'src', 'index.ts');
  const content = fs.readFileSync(indexPath, 'utf-8');

  it('F10-1: Re-exports SeedLab and LiveJamlIde components', () => {
    assert.ok(content.includes('SeedLab'), 'Must re-export SeedLab');
    assert.ok(content.includes('LiveJamlIde'), 'Must re-export LiveJamlIde');
  });

  it('F10-2: Re-exports JamlIde, ResultsView, and TallyBar', () => {
    assert.ok(content.includes('JamlIde'), 'Must re-export JamlIde');
    assert.ok(content.includes('ResultsView'), 'Must re-export ResultsView');
    assert.ok(content.includes('TallyBar'), 'Must re-export TallyBar');
  });

  it('F10-3: Re-exports Jamlyzer, JamlyzerView, and JamlyzerBulk', () => {
    assert.ok(content.includes('Jamlyzer'), 'Must re-export Jamlyzer');
    assert.ok(content.includes('JamlyzerView'), 'Must re-export JamlyzerView');
    assert.ok(content.includes('JamlyzerBulk'), 'Must re-export JamlyzerBulk');
  });

  it('F10-4: Re-exports DailyRitualView and parseRitual helper', () => {
    assert.ok(content.includes('DailyRitualView'), 'Must re-export DailyRitualView');
    assert.ok(content.includes('parseRitual'), 'Must re-export parseRitual');
  });

  it('F10-5: Re-exports DeckStakePicker and deck/stake constant arrays', () => {
    assert.ok(content.includes('DeckStakePicker'), 'Must re-export DeckStakePicker');
    assert.ok(content.includes('DECK_PICKER_NAMES') || content.includes('DECKS'), 'Must export deck names');
  });

  it('F10-6: Re-exports JimboBalatroFooter and JamlGameCard / GameCard', () => {
    assert.ok(content.includes('JimboBalatroFooter'), 'Must re-export JimboBalatroFooter');
    assert.ok(content.includes('JamlGameCard') || content.includes('GameCard'), 'Must re-export GameCard');
  });
});
