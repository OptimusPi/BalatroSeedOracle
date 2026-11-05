// tests/e2e/tier3_combinations/c07_motely_party_to_desktop_party.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  runDesktopTestsFilter,
  runMotelyTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C07: Motely.Party Worker Client <-> BalatroSeedOracle Party Search Service', () => {
  it('C07-1: Desktop SearchTab exposes editable PartyCoordinatorUrlBox two-way bound to SearchModalViewModel', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchTab_ExposesEditablePartyCoordinatorUrlBox');
    assert.equal(res.exitCode, 0, `SearchTab PartyCoordinatorUrl binding test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C07-2: Motely.Tests PartyWorkerTests verify real lease, sweep, report, and cancellation against mock/live HTTP', () => {
    const res = runMotelyTestsFilter('FullyQualifiedName~PartyWorkerTests');
    assert.equal(res.exitCode, 0, `PartyWorkerTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C07-3: BalatroSeedOracle PartySearchService_LifecycleAndCancellation executes start, progress event, and stop', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~PartySearchService_LifecycleAndCancellation');
    assert.equal(res.exitCode, 0, `PartySearchService lifecycle test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C07-4: PartySearchService cleans up IsWorking=false when coordinator URL throws UriFormatException', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~PartySearchService_InvalidCoordinatorUrl_LeavesIsWorkingFalse');
    assert.equal(res.exitCode, 0, `PartySearchService invalid URL test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C07-5: PartySearchService concurrent start/stop stress harness completes without deadlock', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~PartySearchService_ConcurrentStartStopRace');
    assert.equal(res.exitCode, 0, `PartySearchService race test failed:\n${res.stdout}\n${res.stderr}`);
  });
});
