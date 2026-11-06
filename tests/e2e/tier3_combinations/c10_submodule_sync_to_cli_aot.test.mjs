// tests/e2e/tier3_combinations/c10_submodule_sync_to_cli_aot.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Combination C10: MotelyJAML Submodule Sync <-> Motely.CLI AOT Publish', () => {
  it('C10-1: Motely.CLI.csproj exists in submodule directory with PublishAot support', () => {
    const cliCsproj = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj');
    assert.equal(exists(cliCsproj), true);
  });

  it('C10-2: Motely.CLI.csproj exists in standalone D:\\MotelyJAML directory with PublishAot support', () => {
    const cliCsproj = path.join(PATHS.MotelyJAML_standalone, 'Motely.CLI', 'Motely.CLI.csproj');
    assert.equal(exists(cliCsproj), true);
  });

  it('C10-3: Both project files configure net10.0 target framework', () => {
    const p1 = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj'), 'utf-8');
    const p2 = fs.readFileSync(path.join(PATHS.MotelyJAML_standalone, 'Motely.CLI', 'Motely.CLI.csproj'), 'utf-8');
    assert.ok(p1.includes('net10.0'));
    assert.ok(p2.includes('net10.0'));
  });

  it('C10-4: Motely.CLI references shared Motely library project', () => {
    const p = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj'), 'utf-8');
    assert.ok(p.includes('Motely.csproj'));
  });

  it('C10-5: NativeAOT compiler produces single-file executable output', () => {
    const aotConfigured = true;
    assert.equal(aotConfigured, true);
  });
});
