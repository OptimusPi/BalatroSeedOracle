# PowerShell script to replace fake color references with real Balatro colors in all .axaml files

$replacements = @{
    'TextLight' = 'White'
    'DropShadow' = 'HoverShadow'
    'VeryDarkBackground' = 'DarkBackground'
    'ControlBorderGrey' = 'ModalBorder'
    'BlueHover' = 'Blue'
    'DarkModalGrey' = 'DarkBackground'
    'GridLineDarkGrey' = 'ModalBorder'
    'InfoTextPrecise' = 'White'
    'RedShadowPrecise' = 'HoverShadow'
    'TabHoverBackground' = 'DarkBackground'
    'SetDefault' = 'ModalGrey'
    'LightGrey' = 'ModalBorder'
    'MediumGrey' = 'ModalGrey'
    'OrangeHover' = 'Orange'
    'GreenHover' = 'Green'
    'PurpleHover' = 'Purple'
    'Grey' = 'ModalGrey'
}

# Get all .axaml files
$files = Get-ChildItem -Path "X:\BalatroSeedOracle\src" -Filter "*.axaml" -Recurse

$totalReplacements = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileReplacements = 0

    foreach ($fake in $replacements.Keys) {
        $real = $replacements[$fake]
        $pattern = "\{StaticResource $fake\}"
        $replacement = "{StaticResource $real}"

        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            $content = $content -replace [regex]::Escape($pattern), $replacement
            $fileReplacements += $matches.Count
        }
    }

    if ($fileReplacements -gt 0) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "$($file.Name): $fileReplacements replacements" -ForegroundColor Green
        $totalReplacements += $fileReplacements
    }
}

Write-Host "`nTotal replacements: $totalReplacements" -ForegroundColor Cyan
