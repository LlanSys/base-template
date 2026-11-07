[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$MudBlazorAssembly
)

$ErrorActionPreference = 'Stop'
$assemblyPath = (Resolve-Path -LiteralPath $MudBlazorAssembly).Path
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType('MudBlazor.Icons+Material+Filled', $true)
$fields = $type.GetFields([Reflection.BindingFlags]'Public,Static') |
    Where-Object { $_.IsLiteral -and $_.FieldType -eq [string] }
$names = [string[]]@($fields.Name)
if ($names.Length -eq 0) {
    throw 'No Material Filled icon keys were found.'
}

[Array]::Sort($names, [StringComparer]::Ordinal)
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$keys = @($names | Where-Object { $seen.Add($_) })
$lines = for ($index = 0; $index -lt $keys.Count; $index += 8) {
    $end = [Math]::Min($index + 7, $keys.Count - 1)
    $keys[$index..$end] -join ' '
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $repoRoot 'src/Shared/BT.SharedKernel/Features/IAM/Menus/MaterialFilledIconKeys.txt'
[IO.File]::WriteAllText($target, ($lines -join [char]10) + [char]10)
"Exported $($keys.Count) Material Filled icon keys."
