#!/usr/bin/env pwsh

Write-Host "Finding Unused Components and Code in BalatroSeedOracle" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

# Get all C# files
$csFiles = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse
$axamlFiles = Get-ChildItem -Path "src" -Filter "*.axaml" -Recurse

Write-Host "`nKnown Duplicate Components:" -ForegroundColor Yellow
Write-Host "1. DeckStakeSelector (duplicate) vs DeckAndStakeSelector (real)" -ForegroundColor Red
Write-Host "2. FilterSpinner (wrapper) vs FilterSelector (real)" -ForegroundColor Red

Write-Host "`nSearching for unused classes..." -ForegroundColor Yellow

# Find all class definitions
$classes = @{}
foreach ($file in $csFiles) {
    $content = Get-Content $file -Raw
    $matches = [regex]::Matches($content, 'public\s+(partial\s+)?class\s+(\w+)')
    foreach ($match in $matches) {
        $className = $match.Groups[2].Value
        if (-not $classes.ContainsKey($className)) {
            $classes[$className] = @{
                File = $file.FullName
                References = 0
                IsPartial = $match.Groups[1].Value -ne ""
            }
        }
    }
}

Write-Host "Found $($classes.Count) classes" -ForegroundColor Green

# Check for references to each class
foreach ($className in $classes.Keys) {
    $searchPattern = "\b$className\b"
    $references = 0
    
    foreach ($file in $csFiles + $axamlFiles) {
        if ($file.FullName -eq $classes[$className].File) { continue }
        $content = Get-Content $file -Raw
        if ($content -match $searchPattern) {
            $references++
        }
    }
    
    $classes[$className].References = $references
}

Write-Host "`nPotentially Unused Classes (0 external references):" -ForegroundColor Yellow
$unusedClasses = $classes.GetEnumerator() | Where-Object { $_.Value.References -eq 0 } | Sort-Object Name
foreach ($class in $unusedClasses) {
    $relativePath = $class.Value.File.Replace($PWD.Path, "").TrimStart("\", "/")
    Write-Host "  - $($class.Key) in $relativePath" -ForegroundColor Red
}

Write-Host "`nClasses with few references (1-2 references):" -ForegroundColor Yellow
$lowRefClasses = $classes.GetEnumerator() | Where-Object { $_.Value.References -ge 1 -and $_.Value.References -le 2 } | Sort-Object Name
foreach ($class in $lowRefClasses) {
    $relativePath = $class.Value.File.Replace($PWD.Path, "").TrimStart("\", "/")
    Write-Host "  - $($class.Key) ($($class.Value.References) refs) in $relativePath" -ForegroundColor DarkYellow
}

# Find duplicate/similar file names
Write-Host "`nPotential Duplicate Files:" -ForegroundColor Yellow
$fileGroups = $csFiles + $axamlFiles | Group-Object { $_.BaseName -replace 'Modal$|Control$|Popup$|Base$', '' } | Where-Object { $_.Count -gt 1 }
foreach ($group in $fileGroups) {
    Write-Host "  Group: $($group.Name)" -ForegroundColor Cyan
    foreach ($file in $group.Group) {
        $relativePath = $file.FullName.Replace($PWD.Path, "").TrimStart("\", "/")
        Write-Host "    - $relativePath" -ForegroundColor Gray
    }
}

Write-Host "`nRecommendations:" -ForegroundColor Green
Write-Host "1. Remove DeckStakeSelector - use DeckAndStakeSelector instead"
Write-Host "2. Consider removing FilterSpinner if it's just a wrapper"
Write-Host "3. Review classes with 0 references - they might be unused"
Write-Host "4. Check duplicate file groups for redundant functionality"