# PreToolUse/Bash: deny destructive git that raw-dogs the repo.
# stdin: PreToolUse JSON. deny via hookSpecificOutput.permissionDecision
$ErrorActionPreference = "Stop"

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try {
    $evt = $raw | ConvertFrom-Json
}
catch {
    exit 0
}

$cmd = $evt.tool_input.command
if (-not $cmd) { exit 0 }

# Patterns: cage §4. Nat must say exact words for these.
$rules = @(
    @{ re = 'git\s+push\s+[^\n]*--force'; why = "force-push blocked (cage). Nat must say: force push" }
    @{ re = 'git\s+push\s+[^\n]*\s-f(\s|$)'; why = "git push -f blocked (cage). Nat must say: force push" }
    @{ re = 'git\s+push\s+--force-with-lease'; why = "force-with-lease blocked (cage). Nat must say: force push" }
    @{ re = 'git\s+reset\s+--hard'; why = "reset --hard blocked (cage)" }
    @{ re = 'git\s+clean\s+-f'; why = "git clean -f blocked (cage)" }
    @{ re = 'git\s+submodule\s+deinit'; why = "submodule deinit blocked (cage)" }
    @{ re = 'git\s+filter-branch'; why = "filter-branch blocked (cage)" }
    @{ re = 'git\s+update-ref\s+-d'; why = "update-ref -d blocked (cage)" }
    @{ re = 'git\s+checkout\s+--orphan'; why = "orphan branch rewrite blocked (cage)" }
    @{ re = 'git\s+rebase\s+[^\n]*--onto\s+'; why = "rebase --onto blocked without Nat; use ticket" }
)

foreach ($r in $rules) {
    if ($cmd -match $r.re) {
        $out = [ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName            = "PreToolUse"
                permissionDecision       = "deny"
                permissionDecisionReason = "CLAUDE-CAGE HOOK: $($r.why). Command was: $cmd"
            }
        }
        $out | ConvertTo-Json -Compress -Depth 6
        exit 0
    }
}

# Vendor Motely at BSO root (history disaster)
if ($cmd -match 'git\s+add\s+[^\n]*\bMotely/' -and $cmd -notmatch 'src[/\\]MotelyJAML') {
    if ($cmd -match '(^|[\s"])Motely(/|\\|"|\s|$)' -and $cmd -notmatch 'src[/\\]MotelyJAML') {
        $out = [ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName            = "PreToolUse"
                permissionDecision       = "deny"
                permissionDecisionReason = "CLAUDE-CAGE HOOK: refuse staging root Motely/. Engine is src/MotelyJAML submodule only."
            }
        }
        $out | ConvertTo-Json -Compress -Depth 6
        exit 0
    }
}

exit 0
