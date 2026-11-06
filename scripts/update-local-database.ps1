[CmdletBinding()]
param(
    [ValidateSet('SqlServer', 'PostgreSql')]
    [string]$Provider = 'SqlServer',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$operation = if ($Apply) { 'local-upgrade' } else { 'local-status' }

Push-Location $repoRoot
try {
    & dotnet run --project tools/BT.DatabaseTools -- $Provider $operation
    if ($LASTEXITCODE -ne 0) {
        throw 'The local database workflow failed. Review the diagnostics above.'
    }
}
finally {
    Pop-Location
}
