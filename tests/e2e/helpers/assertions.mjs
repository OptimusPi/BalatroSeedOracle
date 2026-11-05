// tests/e2e/helpers/assertions.mjs
// Custom assertion utilities for opaque-box test suites
import assert from 'node:assert/strict';
import fs from 'node:fs';

export function assertValidSeed(seed, message = '') {
  assert.match(seed, /^[1-9A-Z]{1,8}$/, message || `Expected valid 1-8 char Balatro seed ([1-9A-Z]), got '${seed}'`);
}

export function assertExitCode(result, expectedCode = 0, message = '') {
  assert.equal(
    result.exitCode,
    expectedCode,
    message || `Command '${result.command}' failed with exit code ${result.exitCode}.\nStderr: ${result.stderr}\nStdout: ${result.stdout}`
  );
}

export function assertFileExists(filePath, message = '') {
  assert.equal(
    fs.existsSync(filePath),
    true,
    message || `Expected file to exist at ${filePath}`
  );
}

export function assertCloseTo(actual, expected, delta = 0.0001, message = '') {
  assert.ok(
    Math.abs(actual - expected) <= delta,
    message || `Expected ${actual} to be close to ${expected} (within ${delta}), diff: ${Math.abs(actual - expected)}`
  );
}
