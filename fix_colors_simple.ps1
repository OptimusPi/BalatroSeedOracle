# Simple PowerShell script to replace fake color references

$files = Get-ChildItem -Path "X:\BalatroSeedOracle\src" -Filter "*.axaml" -Recurse

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $original = $content

    $content = $content.Replace('{StaticResource TextLight}', '{StaticResource White}')
    $content = $content.Replace('{StaticResource DropShadow}', '{StaticResource HoverShadow}')
    $content = $content.Replace('{StaticResource VeryDarkBackground}', '{StaticResource DarkBackground}')
    $content = $content.Replace('{StaticResource ControlBorderGrey}', '{StaticResource ModalBorder}')
    $content = $content.Replace('{StaticResource BlueHover}', '{StaticResource Blue}')
    $content = $content.Replace('{StaticResource DarkModalGrey}', '{StaticResource DarkBackground}')
    $content = $content.Replace('{StaticResource GridLineDarkGrey}', '{StaticResource ModalBorder}')
    $content = $content.Replace('{StaticResource InfoTextPrecise}', '{StaticResource White}')
    $content = $content.Replace('{StaticResource RedShadowPrecise}', '{StaticResource HoverShadow}')
    $content = $content.Replace('{StaticResource TabHoverBackground}', '{StaticResource DarkBackground}')
    $content = $content.Replace('{StaticResource SetDefault}', '{StaticResource ModalGrey}')
    $content = $content.Replace('{StaticResource LightGrey}', '{StaticResource ModalBorder}')
    $content = $content.Replace('{StaticResource MediumGrey}', '{StaticResource ModalGrey}')
    $content = $content.Replace('{StaticResource OrangeHover}', '{StaticResource Orange}')
    $content = $content.Replace('{StaticResource GreenHover}', '{StaticResource Green}')
    $content = $content.Replace('{StaticResource PurpleHover}', '{StaticResource Purple}')
    $content = $content.Replace('{StaticResource Grey}', '{StaticResource ModalGrey}')

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $content)
        Write-Host "Updated: $($file.Name)" -ForegroundColor Green
    }
}
