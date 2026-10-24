param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "releases"
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Get-Version($project) {
    $xml = Get-Content $project -Raw
    if ($xml -match "<Version>([^<]+)</Version>") { return $Matches[1] }
    return "0.0.0"
}

$mods = "MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago"
$skip = "ScriptHookVDotNet*.dll", "LemonUI*.dll", "MissionLauncher.dll", "GameEnvironment.dll", "GameTraps.dll", "GameCollectibles.dll", "*.pdb", "*.xml"

foreach ($mod in $mods) {
    $project = Join-Path $root "$mod\$mod.csproj"
    $build = Join-Path $root "$mod\bin\$Configuration\net48"
    if (-not (Test-Path $build)) { throw "Build $mod in $Configuration first ($build is missing)." }
    $version = Get-Version $project
    $stage = Join-Path $env:TEMP "gtav-release-$mod"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path $stage | Out-Null
    Get-ChildItem $build -File | Where-Object {
        $name = $_.Name
        ($name -eq "$mod.dll") -or -not ($skip | Where-Object { $name -like $_ })
    } | Copy-Item -Destination $stage
    $data = Join-Path $build $mod
    if (Test-Path $data) { Copy-Item $data -Destination $stage -Recurse }
    $zip = Join-Path $out "$mod-$version.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
    Remove-Item $stage -Recurse -Force
    Write-Host "$zip"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source (Join-Path $root "GameArchipelago\tools\build_apworld.py")
}
$world = Join-Path $root "GameArchipelago\apworld\gtav.apworld"
if (Test-Path $world) {
    Copy-Item $world -Destination $out -Force
    Write-Host (Join-Path $out "gtav.apworld")
}

$setup = Join-Path $root "GTAVArchipelagoSetup\bin\$Configuration\net48"
if (Test-Path $setup) {
    $version = Get-Version (Join-Path $root "GTAVArchipelagoSetup\GTAVArchipelagoSetup.csproj")
    $zip = Join-Path $out "GTAVArchipelagoSetup-$version.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path (Join-Path $setup "GTAVArchipelagoSetup.exe"), (Join-Path $setup "sources.json") -DestinationPath $zip
    Write-Host "$zip"
}
