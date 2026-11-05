// tests/e2e/tier3_combinations/c10_submodule_sync_to_cli_aot.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import crypto from 'node:crypto';
import os from 'node:os';
import { PATHS, exists } from '../helpers/env.mjs';
import { runMotelyCli } from '../helpers/runtime_oracle.mjs';

function sha256File(p) {
  const normalized = fs.readFileSync(p, 'utf-8').replace(/\r\n/g, '\n');
  return crypto.createHash('sha256').update(normalized).digest('hex');
}

describe('Combination C10: MotelyJAML Submodule Sync <-> Motely.CLI Execution & AOT Readiness', () => {
  const subCliCsproj = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj');
  const standaloneCliCsproj = path.join(PATHS.MotelyJAML_standalone, 'Motely.CLI', 'Motely.CLI.csproj');

  it('C10-1: Motely.CLI.csproj exists in submodule and standalone directories with byte-identical source sync', () => {
    assert.equal(exists(subCliCsproj), true);
    assert.equal(exists(standaloneCliCsproj), true);
    assert.equal(sha256File(subCliCsproj), sha256File(standaloneCliCsproj));
  });

  it('C10-2: Core Motely engine files (Motely.csproj & MotelyJamlyzer.cs & JamlConfigLoader.cs) are byte-synchronized across standalone and submodule', () => {
    const pairs = [
      ['Motely', 'Motely.csproj'],
      ['Motely', 'Analysis', 'MotelyJamlyzer.cs'],
      ['Motely', 'Filters', 'JamlConfigLoader.cs'],
    ];
    for (const rel of pairs) {
      const p1 = path.join(PATHS.MotelyJAML, ...rel);
      const p2 = path.join(PATHS.MotelyJAML_standalone, ...rel);
      assert.equal(sha256File(p1), sha256File(p2), `Submodule drift detected in ${rel.join('/')}`);
    }
  });

  it('C10-3: Both Motely.CLI project files configure net10.0 target framework and PublishAot', () => {
    const p1 = fs.readFileSync(subCliCsproj, 'utf-8');
    const p2 = fs.readFileSync(standaloneCliCsproj, 'utf-8');
    assert.ok(p1.includes('net10.0'));
    assert.ok(p2.includes('net10.0'));
    assert.ok(p1.includes('PublishAot'));
  });

  it('C10-4: Motely.CLI references shared Motely library project', () => {
    const p = fs.readFileSync(subCliCsproj, 'utf-8');
    assert.ok(p.includes('Motely.csproj'));
  });

  it('C10-5: Built Motely.CLI executes real JAML digit-leading joker search (`--jaml ... --seeds ...`) cleanly', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'bso-c10-'));
    const jamlFile = path.join(tmpDir, 'c10.jaml');
    try {
      fs.writeFileSync(
        jamlFile,
        'name: C10 Check\ndeck: Red\nstake: White\nshould:\n  - joker: "8 Ball"\n    antes: [1]\n    score: 10\n',
        'utf-8'
      );
      const res = runMotelyCli(['--jaml', jamlFile, '--seeds', 'ALEEB']);
      assert.equal(res.exitCode, 0, `Motely.CLI execution failed:\n${res.stdout}\n${res.stderr}`);
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});
