param([switch]$Test, [switch]$UiTest, [string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'bin' }
$outputDir = [IO.Path]::GetFullPath($OutputDirectory)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compiler)) { throw 'Install .NET Framework 4.8 first.' }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$refs = @('/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Xml.dll')
& $compiler /nologo /utf8output /target:winexe /platform:anycpu /optimize+ ("/out:" + (Join-Path $outputDir '大肥鱼代抽1.0.exe')) $refs $sources
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$runtimeAssets = Join-Path $projectRoot 'assets\runtime'
if (!(Test-Path -LiteralPath (Join-Path $runtimeAssets 'items\rice.png'))) { throw 'Required runtime assets are missing.' }
$assetDestination = Join-Path $outputDir 'assets'
New-Item -ItemType Directory -Force -Path $assetDestination | Out-Null
Get-ChildItem -LiteralPath $runtimeAssets | Copy-Item -Destination $assetDestination -Recurse -Force
Write-Host ('Built: ' + (Join-Path $outputDir '大肥鱼代抽1.0.exe'))
if ($Test) {
    & $compiler /nologo /utf8output /target:exe ("/out:" + (Join-Path $outputDir 'CoreTests.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Xml.dll (Join-Path $projectRoot 'src\Core.cs') (Join-Path $projectRoot 'src\Storage.cs') (Join-Path $projectRoot 'tests\CoreTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Core tests build failed.' }
    & (Join-Path $outputDir 'CoreTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    & $compiler /nologo /utf8output /target:exe /main:AssetTests ("/out:" + (Join-Path $outputDir 'AssetTests.exe')) $refs $sources (Join-Path $projectRoot 'tests\AssetTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Asset tests build failed.' }
    & (Join-Path $outputDir 'AssetTests.exe') (Join-Path $outputDir 'asset-review')
    if ($LASTEXITCODE -ne 0) { throw 'Asset tests failed.' }
}
if ($UiTest) {
    & $compiler /nologo /utf8output /target:winexe /define:UI_TESTS ("/out:" + (Join-Path $outputDir 'UiTests.exe')) $refs $sources
    if ($LASTEXITCODE -ne 0) { throw 'UI tests build failed.' }
    $checkData = Join-Path $outputDir ('ui-check-' + [Guid]::NewGuid().ToString('N'))
    $process = Start-Process -FilePath (Join-Path $outputDir 'UiTests.exe') -ArgumentList ('--smoke-test --data-dir "' + $checkData + '"') -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(60000)) { $process.Kill(); throw 'UI tests timed out.' }
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $checkData 'smoke-test-ok.txt'))) {
        if (Test-Path -LiteralPath (Join-Path $checkData 'smoke-test-error.txt')) { Get-Content -LiteralPath (Join-Path $checkData 'smoke-test-error.txt') }
        throw 'UI tests failed.'
    }
    Get-Content -LiteralPath (Join-Path $checkData 'smoke-test-ok.txt')
}
