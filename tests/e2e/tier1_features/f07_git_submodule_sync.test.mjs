// tests/e2e/tier1_features/f07_git_submodule_sync.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 7: Git Submodule Synchronization (D:\\MotelyJAML <-> src\\MotelyJAML)', () => {
  it('F07-1: Submodule directory exists in BalatroSeedOracle\\src\\MotelyJAML', () => {
    assert.equal(exists(PATHS.MotelyJAML), true, `Path not found: ${PATHS.MotelyJAML}`);
  });

  it('F07-2: Standalone Motely repository exists at D:\\MotelyJAML', () => {
    assert.equal(exists(PATHS.MotelyJAML_standalone), true, `Path not found: ${PATHS.MotelyJAML_standalone}`);
  });

  it('F07-3: Core Motely project file exists identically in both locations', () => {
    const subCsproj = path.join(PATHS.MotelyJAML, 'Motely', 'Motely.csproj');
    const standCsproj = path.join(PATHS.MotelyJAML_standalone, 'Motely', 'Motely.csproj');
    assert.equal(exists(subCsproj), true, `Submodule csproj missing: ${subCsproj}`);
    assert.equal(exists(standCsproj), true, `Standalone csproj missing: ${standCsproj}`);
  });

  it('F07-4: GpuSeedSearcher.cs exists in both standalone and submodule with identical normalized content', () => {
    const subGpu = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    const standGpu = path.join(PATHS.MotelyJAML_standalone, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    assert.equal(exists(subGpu), true, `Submodule GPU missing: ${subGpu}`);
    assert.equal(exists(standGpu), true, `Standalone GPU missing: ${standGpu}`);
    const s1 = fs.readFileSync(subGpu, 'utf-8').replace(/\r\n/g, '\n');
    const s2 = fs.readFileSync(standGpu, 'utf-8').replace(/\r\n/g, '\n');
    assert.equal(s1, s2, 'GpuSeedSearcher.cs must be synchronized between D:\\MotelyJAML and src\\MotelyJAML');
  });

  it('F07-5: Motely.slnx solution file exists in both repository locations', () => {
    const subSln = path.join(PATHS.MotelyJAML, 'Motely.slnx');
    const standSln = path.join(PATHS.MotelyJAML_standalone, 'Motely.slnx');
    assert.equal(exists(subSln), true, `Submodule slnx missing: ${subSln}`);
    assert.equal(exists(standSln), true, `Standalone slnx missing: ${standSln}`);
  });

  it('F07-6: Root .gitmodules in BalatroSeedOracle maps MotelyJAML submodule path', () => {
    const gitmodules = path.join(PATHS.BalatroSeedOracle, '.gitmodules');
    assert.equal(exists(gitmodules), true);
    const content = fs.readFileSync(gitmodules, 'utf-8');
    assert.ok(content.includes('MotelyJAML'), 'Expected .gitmodules to reference MotelyJAML');
  });

  it('F07-7: Every C# source file under Motely/ is byte-for-byte identical (LF-normalized) between D:\\MotelyJAML and src\\MotelyJAML', () => {
    function walkCs(rootDir, rel = '') {
      const entries = fs.readdirSync(path.join(rootDir, rel), { withFileTypes: true });
      let out = [];
      for (const e of entries) {
        if (e.name === 'bin' || e.name === 'obj' || e.name === '.git') continue;
        const childRel = path.join(rel, e.name);
        if (e.isDirectory()) out = out.concat(walkCs(rootDir, childRel));
        else if (e.isFile() && e.name.endsWith('.cs')) out.push(childRel);
      }
      return out.sort();
    }

    const standDir = path.join(PATHS.MotelyJAML_standalone, 'Motely');
    const subDir = path.join(PATHS.MotelyJAML, 'Motely');
    const standFiles = walkCs(standDir);
    const subFiles = walkCs(subDir);
    assert.deepEqual(subFiles, standFiles, 'Submodule Motely/ C# file list drifted from standalone D:\\MotelyJAML');

    for (const rel of standFiles) {
      const a = fs.readFileSync(path.join(standDir, rel), 'utf-8').replace(/\r\n/g, '\n');
      const b = fs.readFileSync(path.join(subDir, rel), 'utf-8').replace(/\r\n/g, '\n');
      assert.equal(b, a, `Submodule drift detected in Motely/${rel}`);
    }
  });
});
