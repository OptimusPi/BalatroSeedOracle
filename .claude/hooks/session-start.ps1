# SessionStart: inject CLAUDE-CAGE into every Claude Code session (Windows).
# stdin: SessionStart JSON (ignored). stdout: hookSpecificOutput.additionalContext
$ErrorActionPreference = "Stop"

$root = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }

# Prefer full engine cage (submodule) over short BSO pointer.
$cageCandidates = @(
    (Join-Path $root "src\MotelyJAML\CLAUDE-CAGE.md"),
    (Join-Path $root "CLAUDE-CAGE.md")
)
$cagePath = $cageCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $cagePath) {
    $ctx = @"
CLAUDE-CAGE MISSING. CODE MULE LAW STILL APPLIES:
- One ticket only. No freestyle.
- No git push --force, reset --hard, clean -fd, submodule deinit, root Motely vendor.
- Proof command exit 0 or STOP.
- Commit only if ticket says COMMIT. Push only if Nat says push.
- Output: Doing | Where | Result | Proof | Next=stop
"@
}
else {
    $body = Get-Content -LiteralPath $cagePath -Raw -Encoding UTF8
    $ctx = "CODE MULE ENFORCED BY HOOK (SessionStart). Read and obey. Architect is Grok; you execute one ticket.`n`n" + $body
}

# Claude caps hook strings ~10k
if ($ctx.Length -gt 9500) {
    $ctx = $ctx.Substring(0, 9500) + "`n`n[CAGE TRUNCATED - open CLAUDE-CAGE.md on disk for full law]"
}

$out = [ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName     = "SessionStart"
        additionalContext = $ctx
    }
}
$out | ConvertTo-Json -Compress -Depth 6
exit 0
