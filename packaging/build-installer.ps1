param([Parameter(Mandatory=$true)][string]$MakeNsis, [string]$OutputDirectory = '', [string]$WorkDirectory = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'dist' }
if (!$WorkDirectory) { $WorkDirectory = Join-Path $projectRoot 'test-output\packaging' }
$outputDir = [IO.Path]::GetFullPath($OutputDirectory)
$workDir = [IO.Path]::GetFullPath($WorkDirectory)
if (!(Test-Path -LiteralPath $MakeNsis -PathType Leaf)) { throw 'Pass the full path to NSIS makensis.exe.' }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
# A new staging directory prevents any stale test programs from being collected.
$stage = Join-Path $workDir ('stage-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $projectRoot 'build.ps1') -OutputDirectory $stage
foreach ($name in @('LICENSE', 'ASSETS_NOTICE.md', 'README.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $stage
}
$uninstallLines = @('; Automatically generated: remove only files installed by this package.')
foreach ($file in Get-ChildItem -LiteralPath $stage -File -Recurse) {
    $relative = $file.FullName.Substring($stage.Length + 1)
    $uninstallLines += ('Delete "$INSTDIR\' + $relative + '"')
}
foreach ($dir in Get-ChildItem -LiteralPath $stage -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending) {
    $relative = $dir.FullName.Substring($stage.Length + 1)
    $uninstallLines += ('RMDir "$INSTDIR\' + $relative + '"')
}
$uninstallList = Join-Path $workDir ('uninstall-' + [Guid]::NewGuid().ToString('N') + '.nsh')
[IO.File]::WriteAllLines($uninstallList, $uninstallLines, (New-Object Text.UTF8Encoding($true)))
$installer = Join-Path $outputDir '大肥鱼代抽1.0.exe'
& $MakeNsis /INPUTCHARSET UTF8 ("/DSTAGE=" + $stage) ("/DOUTPUT=" + $installer) ("/DUNINSTALL_LIST=" + $uninstallList) (Join-Path $PSScriptRoot 'installer.nsi')
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
Get-FileHash -LiteralPath $installer -Algorithm SHA256 | Format-List
Write-Host ('Installer: ' + $installer)
