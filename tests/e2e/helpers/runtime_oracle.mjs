// tests/e2e/helpers/runtime_oracle.mjs
// Shared live-execution harness for WASM, Motely.MCP HTTP/JSON-RPC, Motely.CLI,
// seedfinder.app, ErraticDeck.app, jaml-ui, and BalatroSeedOracle.Tests.
import path from 'node:path';
import fs from 'node:fs';
import os from 'node:os';
import net from 'node:net';
import { spawn } from 'node:child_process';
import { pathToFileURL } from 'node:url';
import { PATHS } from './env.mjs';
import { runCli } from './cli_runner.mjs';

let _wasmPromise = null;
export async function getMotelyWasm() {
  if (!_wasmPromise) {
    _wasmPromise = (async () => {
      const wasmIndex = path.join(PATHS.MotelyJAML, 'Motely.Wasm', 'bin', 'motely-wasm', 'index.mjs');
      const mod = await import(pathToFileURL(wasmIndex).href);
      const bootsharp = mod.default;
      if (bootsharp.getStatus() !== bootsharp.BootStatus.Booted) {
        await bootsharp.boot();
      }
      return { bootsharp, ...mod };
    })();
  }
  return _wasmPromise;
}

export async function runWasmSeedSearch(jamlText, seeds, options = {}) {
  const { JamlConfigLoader, JamlSearchBuilder, MotelyDeck, MotelyStake } = await getMotelyWasm();
  const config = JamlConfigLoader.fromJaml(jamlText);
  let settings = JamlSearchBuilder.createSettings(config, options.minScore ?? null)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(seeds);

  if (options.deck !== undefined && MotelyDeck[options.deck] !== undefined) {
    settings = settings.withDeck(MotelyDeck[options.deck]);
  }
  if (options.stake !== undefined && MotelyStake[options.stake] !== undefined) {
    settings = settings.withStake(MotelyStake[options.stake]);
  }

  const matches = [];
  const scored = [];
  if (settings.seedScoreDesc != null) {
    settings = settings.withScoredResultCallback((tally) => {
      matches.push(tally.seed);
      scored.push({ seed: tally.seed, score: tally.score });
    });
  } else {
    settings = settings.withSeedMatchCallback((seed) => {
      matches.push(seed);
      scored.push({ seed, score: 0 });
    });
  }

  const search = settings.start();
  try {
    await search.waitForCompletionAsync();
    return {
      matches,
      scored,
      totalSeedsSearched: Number(search.totalSeedsSearched),
      matchingSeeds: Number(search.matchingSeeds),
    };
  } finally {
    search.dispose();
  }
}

export async function analyzeSeedWithWasm(seed, jamlOrOptions = {}) {
  const { JamlConfigLoader, MotelyJamlyzer } = await getMotelyWasm();
  let jamlText;
  if (typeof jamlOrOptions === 'string') {
    jamlText = jamlOrOptions;
  } else {
    const deck = jamlOrOptions.deck ?? 'Red';
    const stake = jamlOrOptions.stake ?? 'White';
    const clauses = jamlOrOptions.extraYaml ?? '';
    jamlText = `name: e2e-analyze\ndeck: ${deck}\nstake: ${stake}\nseeds:\n  - ${JSON.stringify(seed)}\n${clauses}`;
  }
  const cfg = JamlConfigLoader.fromJaml(jamlText);
  if (!cfg.seeds || cfg.seeds.length === 0) {
    const seededYaml = `${jamlText.trimEnd()}\nseeds:\n  - ${JSON.stringify(seed)}\n`;
    return MotelyJamlyzer.analyze(JamlConfigLoader.fromJaml(seededYaml))[0];
  }
  return MotelyJamlyzer.analyze(cfg)[0];
}

let _mcpPromise = null;
export async function getMcpServer() {
  if (!_mcpPromise) {
    _mcpPromise = startEphemeralMcpServer();
  }
  return _mcpPromise;
}

async function startEphemeralMcpServer() {
  const mcpDll = path.join(PATHS.MotelyJAML, 'Motely.MCP', 'bin', 'Release', 'net10.0', 'Motely.MCP.dll');
  const mcpDebugDll = path.join(PATHS.MotelyJAML, 'Motely.MCP', 'bin', 'Debug', 'net10.0', 'Motely.MCP.dll');
  let targetDll = fs.existsSync(mcpDll) ? mcpDll : mcpDebugDll;
  if (!fs.existsSync(targetDll)) {
    const buildRes = runCli('dotnet', ['build', path.join(PATHS.MotelyJAML, 'Motely.MCP', 'Motely.MCP.csproj'), '-c', 'Release', '--nologo'], {
      cwd: PATHS.MotelyJAML,
      timeoutMs: 60000,
    });
    if (buildRes.exitCode !== 0) {
      throw new Error(`Failed to build Motely.MCP: ${buildRes.stderr || buildRes.stdout}`);
    }
    targetDll = mcpDll;
  }

  const port = await new Promise((resolve, reject) => {
    const srv = net.createServer();
    srv.listen(0, '127.0.0.1', () => {
      const p = srv.address().port;
      srv.close(() => resolve(p));
    });
    srv.on('error', reject);
  });

  const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'bso-e2e-mcp-'));
  const baseUrl = `http://127.0.0.1:${port}`;
  const proc = spawn('dotnet', [targetDll, '--urls', baseUrl], {
    cwd: PATHS.MotelyJAML,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      MOTELY_DATA_DIR: dataDir,
      MOTELY_LAKE_PATH: PATHS.dungmot,
      MOTELY_LAKE_DIR: PATHS.dungmot,
    },
    stdio: 'ignore',
  });
  proc.unref();

  const cleanup = () => {
    try { proc.kill(); } catch {}
    try { fs.rmSync(dataDir, { recursive: true, force: true }); } catch {}
  };
  process.on('exit', cleanup);

  let healthy = false;
  for (let i = 0; i < 60; i++) {
    try {
      const r = await fetch(`${baseUrl}/health`);
      if (r.ok) {
        healthy = true;
        break;
      }
    } catch {}
    await new Promise(r => setTimeout(r, 100));
  }
  if (!healthy) {
    cleanup();
    throw new Error(`Motely.MCP failed to become healthy at ${baseUrl}`);
  }

  // Perform MCP JSON-RPC initialize handshake once
  let sessionId = null;
  const mcpHeaders = () => ({
    'Content-Type': 'application/json',
    'Accept': 'application/json, text/event-stream',
    ...(sessionId ? { 'Mcp-Session-Id': sessionId } : {}),
  });

  const parseMcpBody = async (res) => {
    const txt = await res.text();
    const trimmed = txt.trim();
    if (trimmed.startsWith('{')) {
      return JSON.parse(trimmed);
    }
    const dataLines = trimmed
      .split(/\r?\n/)
      .filter(l => l.startsWith('data:'))
      .map(l => l.slice(5).trim())
      .filter(Boolean);
    if (dataLines.length > 0) {
      return JSON.parse(dataLines[dataLines.length - 1]);
    }
    throw new Error(`Unexpected MCP response (status ${res.status}): ${txt.slice(0, 300)}`);
  };

  const initRes = await fetch(`${baseUrl}/mcp`, {
    method: 'POST',
    headers: mcpHeaders(),
    body: JSON.stringify({
      jsonrpc: '2.0',
      id: 1,
      method: 'initialize',
      params: {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: { name: 'bso-e2e-suite', version: '1.0.0' },
      },
    }),
  });
  sessionId = initRes.headers.get('mcp-session-id');
  const initJson = await parseMcpBody(initRes);

  await fetch(`${baseUrl}/mcp`, {
    method: 'POST',
    headers: mcpHeaders(),
    body: JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }),
  });

  let nextId = 2;
  return {
    baseUrl,
    initJson,
    async rootInfo() {
      return (await fetch(`${baseUrl}/`)).json();
    },
    async listTools() {
      const res = await fetch(`${baseUrl}/mcp`, {
        method: 'POST',
        headers: mcpHeaders(),
        body: JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/list', params: {} }),
      });
      const rpc = await parseMcpBody(res);
      return rpc.result?.tools ?? [];
    },
    async callTool(name, args = {}) {
      const res = await fetch(`${baseUrl}/mcp`, {
        method: 'POST',
        headers: mcpHeaders(),
        body: JSON.stringify({
          jsonrpc: '2.0',
          id: nextId++,
          method: 'tools/call',
          params: { name, arguments: args },
        }),
      });
      return parseMcpBody(res);
    },
    async rawMcpPost(rawBody) {
      const res = await fetch(`${baseUrl}/mcp`, {
        method: 'POST',
        headers: mcpHeaders(),
        body: rawBody,
      });
      const text = await res.text();
      return { status: res.status, text };
    },
    async createParty(req) {
      const payload = req?.jaml && !req?.filterOrJaml ? { ...req, filterOrJaml: req.jaml } : req;
      const res = await fetch(`${baseUrl}/api/party/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      const txt = await res.text();
      return { status: res.status, body: txt.trim().startsWith('{') ? JSON.parse(txt) : { error: txt } };
    },
    async nextPartyLease(partyId, blocks) {
      const qs = new URLSearchParams();
      if (partyId) qs.set('partyId', partyId);
      if (blocks !== undefined) qs.set('blocks', String(blocks));
      const res = await fetch(`${baseUrl}/api/party/next?${qs.toString()}`);
      const txt = await res.text();
      return { status: res.status, body: txt.trim().startsWith('{') ? JSON.parse(txt) : { error: txt } };
    },
    async reportParty(payload) {
      const res = await fetch(`${baseUrl}/api/party/report`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      const txt = await res.text();
      return { status: res.status, body: txt.trim().startsWith('{') ? JSON.parse(txt) : { error: txt } };
    },
    async getPartyStatus(partyId) {
      const res = await fetch(`${baseUrl}/api/party/${encodeURIComponent(partyId)}`);
      const txt = await res.text();
      return { status: res.status, body: txt.trim().startsWith('{') ? JSON.parse(txt) : { error: txt } };
    },
    async sweepParty(partyId, { blocks = 10, gpu = true, threads } = {}) {
      const qs = new URLSearchParams({ blocks: String(blocks), gpu: String(gpu) });
      if (threads !== undefined) qs.set('threads', String(threads));
      const res = await fetch(`${baseUrl}/api/party/${encodeURIComponent(partyId)}/sweep?${qs.toString()}`, {
        method: 'POST',
      });
      const txt = await res.text();
      return { status: res.status, body: txt.trim().startsWith('{') ? JSON.parse(txt) : { error: txt } };
    },
  };
}

export function runMotelyCli(args = [], options = {}) {
  const cliReleaseDll = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'bin', 'Release', 'net10.0', 'Motely.CLI.dll');
  const cliDebugDll = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'bin', 'Debug', 'net10.0', 'Motely.CLI.dll');
  const dll = fs.existsSync(cliReleaseDll) ? cliReleaseDll : cliDebugDll;
  return runCli('dotnet', [dll, ...args], {
    cwd: options.cwd ?? PATHS.MotelyJAML,
    timeoutMs: options.timeoutMs ?? 45000,
    env: options.env,
  });
}

let _desktopTestSummary = null;
export function getDesktopTestSuiteSummary() {
  if (!_desktopTestSummary) {
    const testCsproj = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle.Tests', 'BalatroSeedOracle.Tests.csproj');
    const testDll = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle.Tests', 'bin', 'Release', 'net10.0', 'BalatroSeedOracle.Tests.dll');
    const args = fs.existsSync(testDll)
      ? ['test', testCsproj, '-c', 'Release', '--no-build', '--nologo']
      : ['test', testCsproj, '-c', 'Release', '--nologo'];
    const res = runCli('dotnet', args, {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 120000,
      env: { DiffEngine_Disabled: 'true' },
    });
    const combined = `${res.stdout}\n${res.stderr}`;
    const m = combined.match(/Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)/);
    _desktopTestSummary = {
      exitCode: res.exitCode,
      failed: m ? Number(m[1]) : -1,
      passed: m ? Number(m[2]) : 0,
      skipped: m ? Number(m[3]) : -1,
      total: m ? Number(m[4]) : 0,
      output: combined,
    };
  }
  return _desktopTestSummary;
}

export function runMotelyTestsFilter(filterExpr, timeoutMs = 60000) {
  const testCsproj = path.join(PATHS.MotelyJAML, 'Motely.Tests', 'Motely.Tests.csproj');
  const testDll = path.join(PATHS.MotelyJAML, 'Motely.Tests', 'bin', 'Release', 'net10.0', 'Motely.Tests.dll');
  const args = fs.existsSync(testDll)
    ? ['test', testCsproj, '-c', 'Release', '--no-build', '--nologo', '--filter', filterExpr]
    : ['test', testCsproj, '-c', 'Release', '--nologo', '--filter', filterExpr];
  return runCli('dotnet', args, {
    cwd: PATHS.MotelyJAML,
    timeoutMs,
    env: { DiffEngine_Disabled: 'true' },
  });
}

export function runDesktopTestsFilter(filterExpr, timeoutMs = 60000) {
  const testCsproj = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle.Tests', 'BalatroSeedOracle.Tests.csproj');
  const testDll = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle.Tests', 'bin', 'Release', 'net10.0', 'BalatroSeedOracle.Tests.dll');
  const args = fs.existsSync(testDll)
    ? ['test', testCsproj, '-c', 'Release', '--no-build', '--nologo', '--filter', filterExpr]
    : ['test', testCsproj, '-c', 'Release', '--nologo', '--filter', filterExpr];
  return runCli('dotnet', args, {
    cwd: PATHS.BalatroSeedOracle,
    timeoutMs,
    env: { DiffEngine_Disabled: 'true' },
  });
}

let _partyStoreMod = null;
export async function getSeedfinderPartyStore() {
  if (!_partyStoreMod) {
    const file = path.join(PATHS.seedfinderApp, 'lib', 'partyStore.ts');
    _partyStoreMod = await import(pathToFileURL(file).href);
  }
  return _partyStoreMod;
}

let _lakeRouteMod = null;
export async function getSeedfinderLakeRoute() {
  if (!_lakeRouteMod) {
    const file = path.join(PATHS.seedfinderApp, 'app', 'api', 'lake', 'route.ts');
    _lakeRouteMod = await import(pathToFileURL(file).href);
  }
  return _lakeRouteMod;
}

let _dailyChallengeMod = null;
export async function getErraticDeckDailyChallenge() {
  if (!_dailyChallengeMod) {
    const file = path.join(PATHS.ErraticDeckApp, 'lib', 'dailyChallenge.ts');
    _dailyChallengeMod = await import(pathToFileURL(file).href);
  }
  return _dailyChallengeMod;
}

let _parquetLakeMod = null;
export async function getErraticDeckParquetLake() {
  if (!_parquetLakeMod) {
    const file = path.join(PATHS.ErraticDeckApp, 'lib', 'parquetLake.ts');
    _parquetLakeMod = await import(pathToFileURL(file).href);
  }
  return _parquetLakeMod;
}

let _jamlUiMods = null;
export async function getJamlUiRuntime() {
  if (!_jamlUiMods) {
    const motd = await import(pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'motd.js')).href);
    const tokens = await import(pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'tokens.js')).href);
    const vocab = await import(pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'vocab.js')).href);
    const ritual = await import(pathToFileURL(path.join(PATHS.jamlUi, 'dist', 'lib', 'daily', 'ritual.js')).href);
    _jamlUiMods = { motd, tokens, vocab, ritual };
  }
  return _jamlUiMods;
}

