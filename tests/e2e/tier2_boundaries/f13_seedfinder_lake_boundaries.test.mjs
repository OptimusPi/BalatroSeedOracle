// tests/e2e/tier2_boundaries/f13_seedfinder_lake_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { DUNGMOT_PATHS } from '../helpers/env.mjs';
import { getSeedfinderLakeRoute } from '../helpers/runtime_oracle.mjs';

describe('Feature 13 Boundaries: seedfinder.app Dungmot Lake Browser', () => {
  it('F13-B1: Unknown partition with no fallback returns clean 200 JSON with empty seeds: [] array', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=ranks&partition=UnknownRank999&limit=10'));
    assert.equal(res.status, 200);
    const body = await res.json();
    assert.deepEqual(body.seeds, []);
    assert.equal(body.count, 0);
  });

  it('F13-B2: Concurrent /api/lake requests across distinct categories resolve independently without cross-talk', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const [rRank, rSuit, rNeg] = await Promise.all([
      GET(new Request('http://localhost/api/lake?type=ranks&partition=10&limit=3')),
      GET(new Request('http://localhost/api/lake?type=suits&partition=Spades&limit=3')),
      GET(new Request('http://localhost/api/lake?type=negatives&partition=uncommon&limit=3')),
    ]);
    const [bRank, bSuit, bNeg] = await Promise.all([rRank.json(), rSuit.json(), rNeg.json()]);
    assert.equal(bRank.seeds[0], '1UQ8D22L');
    assert.equal(bSuit.seeds[0], '113L2V3N');
    assert.equal(bNeg.seeds[0], '111S8CHT');
  });

  it('F13-B3: /api/lake strips CRLF whitespace and CSV count suffixes from returned seed strings', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=negatives&partition=uncommon&limit=10'));
    const body = await res.json();
    for (const s of body.seeds) {
      assert.equal(s.includes(','), false);
      assert.equal(s, s.trim());
      assert.match(s, /^[1-9A-Z]{8}$/);
    }
  });

  it('F13-B4: Excessive limit query parameter (limit=99999) is capped to <= 500 by /api/lake', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const res = await GET(new Request('http://localhost/api/lake?type=negatives&partition=uncommon&limit=99999'));
    const body = await res.json();
    assert.ok(body.seeds.length <= 500, `Expected /api/lake limit cap <= 500, got ${body.seeds.length}`);
    assert.equal(body.count, body.seeds.length);
  });

  it('F13-B5: XSS / path-traversal partition parameter (<script>alert(1)</script> or ../etc) returns HTTP 400', async () => {
    const { GET } = await getSeedfinderLakeRoute();
    const xssRes = await GET(new Request('http://localhost/api/lake?type=ranks&partition=%3Cscript%3Ealert(1)%3C/script%3E'));
    assert.equal(xssRes.status, 400);

    const traversalRes = await GET(new Request('http://localhost/api/lake?type=ranks&partition=..%2F..%2Fsecret'));
    assert.equal(traversalRes.status, 400);
  });

  it('F13-B6: Raw uncommonss.csv row for 111S8CHT records >= 3 Negative Uncommon jokers', () => {
    const lines = fs.readFileSync(DUNGMOT_PATHS.uncommonCsv, 'utf-8').split(/\r?\n/);
    const row = lines.find(l => l.startsWith('111S8CHT,'));
    assert.ok(row);
    const count = Number.parseInt(row.split(',')[1], 10);
    assert.equal(count, 3);
  });
});
