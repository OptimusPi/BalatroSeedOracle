// tests/e2e/helpers/cli_runner.mjs
// Process execution helper for opaque-box CLI tests
import { spawnSync } from 'node:child_process';

export function runCli(command, args = [], options = {}) {
  const timeoutMs = options.timeoutMs || 30000;
  const cwd = options.cwd || process.cwd();
  const env = { ...process.env, ...(options.env || {}) };

  const start = Date.now();
  const result = spawnSync(command, args, {
    cwd,
    env,
    encoding: 'utf-8',
    timeout: timeoutMs,
    shell: true,
  });

  const durationMs = Date.now() - start;

  return {
    command: `${command} ${args.join(' ')}`,
    exitCode: result.status,
    stdout: result.stdout || '',
    stderr: result.stderr || '',
    durationMs,
    timedOut: result.error && result.error.code === 'ETIMEDOUT',
    error: result.error,
  };
}
