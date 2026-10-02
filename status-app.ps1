param(
    [string]$Url = "http://localhost:5156"
)

$manager = Join-Path $PSScriptRoot "manage-app.ps1"
& $manager -Action Status -Url $Url
exit $LASTEXITCODE
