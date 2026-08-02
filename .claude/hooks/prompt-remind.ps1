# UserPromptSubmit: short mule reminder every turn (does not replace user prompt).
$ErrorActionPreference = "Stop"

$ctx = @"
[CAGE] CODE MULE: one ticket · listed files only · proof exit 0 · no force-push/reset --hard/clean -fd · no root Motely vendor · Doing/Where/Result/Proof/Next=stop · if no ticket: STOP and ask ticket id?
"@

$out = [ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName     = "UserPromptSubmit"
        additionalContext = $ctx
    }
}
$out | ConvertTo-Json -Compress -Depth 6
exit 0
