param(
    [string]$Game = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Get-ChildItem $root -Recurse -File | Unblock-File -ErrorAction SilentlyContinue

if (-not $Game) {
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
        "C:\Program Files\Epic Games\GTAV",
        "C:\Program Files\Rockstar Games\Grand Theft Auto V"
    )
    $steam = Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue
    if ($steam -and $steam.SteamPath) {
        $library = Join-Path ($steam.SteamPath -replace "/", "\") "steamapps\libraryfolders.vdf"
        if (Test-Path $library) {
            foreach ($match in [regex]::Matches((Get-Content $library -Raw), '"path"\s+"([^"]+)"')) {
                $candidates += (Join-Path ($match.Groups[1].Value -replace "\\\\", "\") "steamapps\common\Grand Theft Auto V")
            }
        }
    }
    $Game = $candidates | Where-Object { Test-Path (Join-Path $_ "GTA5.exe") } | Select-Object -First 1
    if (-not $Game) { throw "GTA V not found. Run: .\Deploy.ps1 -Game 'D:\path\to\Grand Theft Auto V'" }
}
$target = Join-Path $Game "Archipelago"
New-Item -ItemType Directory -Force -Path $target | Out-Null
Write-Host "Game folder: $Game"

$process = Get-Process GTA5 -ErrorAction SilentlyContinue
if ($process) { throw "Close GTA V first: the mods cannot be replaced while the game runs." }

$solution = Join-Path $root "GTAV-Archipelago.sln"
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    & $dotnet.Source build $solution -c $Configuration -nologo -v minimal
}
else {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { throw "Neither dotnet nor Visual Studio was found." }
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if (-not $msbuild) { throw "MSBuild was not found in Visual Studio." }
    & $msbuild $solution /restore /p:Configuration=$Configuration /m /v:minimal /nologo
}
if ($LASTEXITCODE -ne 0) { throw "The build failed, see the errors above." }

$mods = "MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago"
$skip = "ScriptHookVDotNet*.dll", "LemonUI*.dll", "*.pdb", "*.xml"
foreach ($mod in $mods) {
    $build = Join-Path $root "$mod\bin\$Configuration\net48"
    if (-not (Test-Path $build)) { throw "$build is missing after the build." }
    Get-ChildItem $build -File | Where-Object {
        $name = $_.Name
        -not ($skip | Where-Object { $name -like $_ })
    } | Copy-Item -Destination $target -Force
    $data = Join-Path $build $mod
    if (Test-Path $data) {
        $destination = Join-Path $target $mod
        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        foreach ($file in Get-ChildItem $data -File) {
            $existing = Join-Path $destination $file.Name
            if ($file.Name -eq "config.json" -and (Test-Path $existing)) { continue }
            Copy-Item $file.FullName -Destination $existing -Force
        }
    }
    Write-Host "Deployed $mod"
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source (Join-Path $root "GameArchipelago\tools\build_apworld.py")
    $world = Join-Path $root "GameArchipelago\apworld\gtav.apworld"
    foreach ($folder in @("C:\ProgramData\Archipelago\custom_worlds", (Join-Path $env:LOCALAPPDATA "Archipelago\custom_worlds"))) {
        if (Test-Path $folder) {
            Copy-Item $world -Destination $folder -Force
            Write-Host "APWorld copied to $folder"
        }
    }
}

Write-Host ""
Write-Host "Done. Your config.json files were kept." -ForegroundColor Green
