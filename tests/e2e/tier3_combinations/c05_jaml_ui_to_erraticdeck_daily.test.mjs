// tests/e2e/tier3_combinations/c05_jaml_ui_to_erraticdeck_daily.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { pathToFileURL } from 'node:url';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getErraticDeckDailyChallenge,
  getMotelyWasm,
  analyzeSeedWithWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C05: jaml-ui DailyRitualView & jimbo.css <-> ErraticDeck.app Daily Challenges', () => {
  it('C05-1: Built jaml-ui dist/index.js exports DailyRitualView and parseRitual', () => {
    const distIndex = path.join(PATHS.jamlUi, 'dist', 'index.js');
    const content = fs.readFileSync(distIndex, 'utf-8');
    assert.ok(content.includes('DailyRitualView'));
    assert.ok(content.includes('parseRitual'));
  });

  it('C05-2: dist/ui/jimbo.css exists and contains non-empty retro CRT CSS rules', () => {
    const cssPath = path.join(PATHS.jamlUi, 'dist', 'ui', 'jimbo.css');
    assert.equal(exists(cssPath), true);
    assert.ok(fs.statSync(cssPath).size > 500);
  });

  it('C05-3: Real parseRitual from jaml-ui/dist/lib/daily/ritual.js parses ErraticDeck.app getDailyChallenge JAML', async () => {
    const { parseRitual } = await import(
      pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'lib', 'daily', 'ritual.js')).href
    );
    const { getDailyChallenge } = await getErraticDeckDailyChallenge();
    const challenge = getDailyChallenge(new Date(Date.UTC(2026, 10, 4)));
    const parsed = parseRitual(`# emoji: ${challenge.emoji}\n# hint: ${challenge.hint}\n${challenge.jaml}`);
    assert.equal(parsed.emoji, challenge.emoji);
    assert.equal(parsed.hint, challenge.hint);
    assert.ok(parsed.title.length > 0);
  });

  it('C05-4: All 7 ErraticDeck.app dailyChallenge JAML templates validate in real motely-wasm', async () => {
    const { getDailyChallenge } = await getErraticDeckDailyChallenge();
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    for (let day = 0; day < 14; day++) {
      const ch = getDailyChallenge(new Date(Date.UTC(2026, 10, 1 + day)));
      assert.equal(JamlConfigLoader.check(ch.jaml), null, `Invalid daily challenge JAML in '${ch.title}'`);
      const cfg = JamlConfigLoader.fromJaml(ch.jaml);
      assert.ok(JamlSearchBuilder.createSettings(cfg));
    }
  });

  it('C05-5: JamlyzerView.js wires JamlyzerErraticDeck and real WASM Erratic analysis returns 52 cards', async () => {
    const viewJs = fs.readFileSync(path.join(PATHS.jamlUi, 'dist', 'components', 'JamlyzerView.js'), 'utf-8');
    assert.ok(viewJs.includes('JamlyzerErraticDeck'), 'JamlyzerView must render JamlyzerErraticDeck');
    const res = await analyzeSeedWithWasm('ALEEB', { deck: 'Erratic', stake: 'White' });
    assert.equal(res.erraticDeck.length, 52);
  });
});
