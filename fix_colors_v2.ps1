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
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileReplacements = 0

    foreach ($fake in $replacements.Keys) {
        $real = $replacements[$fake]
        $pattern = [regex]::Escape("{StaticResource $fake}")
        $replacement = "{StaticResource $real}"

        $newContent = $content -creplace $pattern, $replacement
        $matches = ($content.Length - $newContent.Length) / ($pattern.Length - $replacement.Length)
        if ($matches -gt 0) {
            $content = $newContent
            $fileReplacements += $matches
        }
    }

    if ($content -ne $originalContent) {
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
        Write-Host "$($file.Name): $fileReplacements replacements" -ForegroundColor Green
        $totalReplacements += $fileReplacements
    }
}

Write-Host "`nTotal replacements: $totalReplacements" -ForegroundColor Cyan
