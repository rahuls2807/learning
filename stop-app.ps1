param(
    [string]$Url = "http://localhost:5156"
)

$manager = Join-Path $PSScriptRoot "manage-app.ps1"
& $manager -Action Stop -Url $Url
exit $LASTEXITCODE
