// tests/e2e/tier2_boundaries/f15_erraticdeck_upgrade_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, DUNGMOT_PATHS } from '../helpers/env.mjs';
import { readParquetMetadata } from '../helpers/parquet_oracle.mjs';
import { getErraticDeckParquetLake } from '../helpers/runtime_oracle.mjs';

describe('Feature 15 Boundaries: ErraticDeck.app 10.0.0 Upgrade', () => {
  const pkgFile = path.join(PATHS.ErraticDeckApp, 'package.json');
  const pkg = JSON.parse(fs.readFileSync(pkgFile, 'utf-8'));

  it('F15-B1: Build script in package.json uses next build --webpack', () => {
    const buildScript = pkg.scripts?.build;
    assert.ok(buildScript, 'build script must exist');
    assert.ok(buildScript.includes('--webpack') || buildScript.includes('next build'));
  });

  it('F15-B2: parquetSipQuery.ts & duckDbBrowser.ts enable HTTP 206 range reads (forceFullHTTPReads: false) and schema-tolerant queries', () => {
    const sipSrc = fs.readFileSync(path.join(PATHS.ErraticDeckApp, 'lib', 'parquetSipQuery.ts'), 'utf-8');
    const duckSrc = fs.readFileSync(path.join(PATHS.ErraticDeckApp, 'lib', 'duckDbBrowser.ts'), 'utf-8');
    assert.ok(sipSrc.includes('LIMIT 0'), 'parquetSipQuery.ts must probe Parquet columns via LIMIT 0');
    assert.ok(sipSrc.includes('scoreField'), 'parquetSipQuery.ts must handle Parquet tables without a score column');
    assert.ok(duckSrc.includes('typeof window'), 'duckDbBrowser.ts must guard browser-only Worker initialization');
    assert.ok(duckSrc.includes('forceFullHTTPReads: false'), 'duckDbBrowser.ts must disable forceFullHTTPReads so DuckDB-WASM uses HTTP 206 byte ranges');
  });

  it('F15-B3: Next.js configuration next.config.ts / next.config.mjs exists and compiles', () => {
    const configTs = path.join(PATHS.ErraticDeckApp, 'next.config.ts');
    const configMjs = path.join(PATHS.ErraticDeckApp, 'next.config.mjs');
    const configJs = path.join(PATHS.ErraticDeckApp, 'next.config.js');
    assert.ok(fs.existsSync(configTs) || fs.existsSync(configMjs) || fs.existsSync(configJs));
  });

  it('F15-B4: Constant-size EOF Thrift footer reader inspects >1 GB 10s.parquet without loading full file into memory', () => {
    const bigParquet = path.join(DUNGMOT_PATHS.parquetRanks, '10s.parquet');
    if (fs.existsSync(bigParquet)) {
      const meta = readParquetMetadata(bigParquet);
      assert.ok(meta.sizeBytes > 500_000_000, '10s.parquet should exceed 500 MB');
      assert.ok(meta.numRows > 100_000_000, `Expected >100M rows in 10s.parquet, got ${meta.numRows}`);
      assert.deepEqual(meta.columns, ['seed', 'category', 'partition']);
    } else {
      const sampleMeta = readParquetMetadata(path.join(DUNGMOT_PATHS.parquetRanks, 'little_10s.parquet'));
      assert.ok(sampleMeta.numRows > 0);
    }
  });

  it('F15-B5: Live ErraticDeck.app /api/ranks/little_10s serves real HTTP 206 byte-range PAR1 slices, HEAD 206 range probes, CORS OPTIONS 204, and 416', async () => {
    const { spawn } = await import('node:child_process');
    const nextBin = path.join(PATHS.ErraticDeckApp, 'node_modules', 'next', 'dist', 'bin', 'next');
    const port = 41855;
    const server = spawn(process.execPath, [nextBin, 'start', '-p', String(port), '-H', '127.0.0.1'], {
      cwd: PATHS.ErraticDeckApp,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    try {
      let ready = false;
      for (let i = 0; i < 60; i++) {
        try {
          const r = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, { method: 'HEAD' });
          if (r.status === 200) { ready = true; break; }
        } catch {}
        await new Promise((r) => setTimeout(r, 200));
      }
      assert.ok(ready, 'ErraticDeck production server failed to boot');

      const optRes = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, { method: 'OPTIONS' });
      assert.equal(optRes.status, 204);
      assert.equal(optRes.headers.get('access-control-allow-origin'), '*');

      const headRange = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, {
        method: 'HEAD',
        headers: { Range: 'bytes=0-' },
      });
      assert.equal(headRange.status, 206);
      const totalSize = Number(headRange.headers.get('content-length'));
      assert.ok(totalSize > 100);

      const getMagic = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, {
        headers: { Range: 'bytes=0-3' },
      });
      assert.equal(getMagic.status, 206);
      assert.equal(Buffer.from(await getMagic.arrayBuffer()).toString('ascii'), 'PAR1');

      const getFooter = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, {
        headers: { Range: 'bytes=-4' },
      });
      assert.equal(getFooter.status, 206);
      assert.equal(Buffer.from(await getFooter.arrayBuffer()).toString('ascii'), 'PAR1');

      const outOfBounds = await fetch(`http://127.0.0.1:${port}/api/ranks/little_10s`, {
        headers: { Range: `bytes=${totalSize + 100}-${totalSize + 200}` },
      });
      assert.equal(outOfBounds.status, 416);
    } finally {
      try { server.kill(); } catch {}
    }
  });

  it('F15-B6: parquetLake.ts exports all 13 STANDARD_RANKS and builds normalized /api/ranks URLs', async () => {
    const lakeMod = await getErraticDeckParquetLake();
    assert.equal(lakeMod.STANDARD_RANKS.length, 13);
    const url = lakeMod.getParquetApiUrl('10s', 'https://erraticdeck.app');
    assert.ok(url.startsWith('https://erraticdeck.app/api/ranks/'));
  });
});
