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

$libs = @{
    "ScriptHookVDotNet3.dll" = "https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases/download/v3.7.0-nightly.195/ScriptHookVDotNet-v3.7.0-nightly.195.zip"
    "LemonUI.SHVDN3.dll" = "https://github.com/LemonUIbyLemon/LemonUI/releases/download/v2.2/LemonUI.zip"
}
$libMods = "MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago"
foreach ($lib in $libs.Keys) {
    $missing = $libMods | Where-Object { -not (Test-Path (Join-Path $root "$_\lib\$lib")) }
    if (-not $missing) { continue }
    $local = @((Join-Path $Game $lib), (Join-Path $target $lib)) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($local) {
        Write-Host "Using $lib from $local"
        foreach ($mod in $libMods) {
            $dir = Join-Path $root "$mod\lib"
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
            Copy-Item $local (Join-Path $dir $lib) -Force
        }
        continue
    }
    $prefix = if ($lib -like "ScriptHookVDotNet*") { "ScriptHookVDotNet" } else { "LemonUI" }
    $downloaded = Get-ChildItem (Join-Path $env:USERPROFILE "Downloads") -Filter "$prefix*.zip" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $keep = $false
    if ($downloaded) {
        Write-Host "Using $lib from $($downloaded.FullName)"
        $zip = $downloaded.FullName
        $keep = $true
    }
    else {
        Write-Host "Downloading $lib ..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $zip = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName() + ".zip")
        for ($try = 1; ; $try++) {
            try {
                if ($try % 2 -eq 0) {
                    Write-Host "  trying through the GitHub API..."
                    $m = [regex]::Match($libs[$lib], 'github\.com/([^/]+/[^/]+)/releases/download/([^/]+)/([^/?#]+)')
                    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$($m.Groups[1].Value)/releases/tags/$($m.Groups[2].Value)" -UseBasicParsing
                    $asset = $release.assets | Where-Object { $_.name -eq $m.Groups[3].Value } | Select-Object -First 1
                    if (-not $asset) { throw "$($m.Groups[3].Value) is not in the release" }
                    Invoke-WebRequest -Uri $asset.url -Headers @{ Accept = "application/octet-stream" } -OutFile $zip -UseBasicParsing
                }
                elseif ($try -eq 1) {
                    Invoke-WebRequest -Uri $libs[$lib] -OutFile $zip -UseBasicParsing
                }
                else {
                    Write-Host "  trying a direct connection (without the Windows proxy settings)..."
                    $client = New-Object Net.WebClient
                    $client.Proxy = $null
                    $client.Headers.Add("User-Agent", "GTAVArchipelago-Deploy")
                    $client.DownloadFile($libs[$lib], $zip)
                }
                break
            }
            catch {
                if ($try -ge 4) { throw "Could not download $lib ($($_.Exception.Message)). Download $($libs[$lib]) in your browser (it stays in Downloads) and run Deploy again." }
                Write-Host "  GitHub did not answer ($($_.Exception.Message)), trying another way..."
                if ($try -ge 2) { Start-Sleep -Seconds (3 * $try) }
            }
        }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $entry = $archive.Entries | Where-Object { $_.Name -ieq $lib } | Sort-Object { $_.FullName.Length } | Select-Object -First 1
        if (-not $entry) { throw "$lib not found in $($libs[$lib])" }
        foreach ($mod in $libMods) {
            $dir = Join-Path $root "$mod\lib"
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $dir $lib), $true)
        }
    }
    finally {
        $archive.Dispose()
        if (-not $keep) { Remove-Item $zip -ErrorAction SilentlyContinue }
    }
}

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
