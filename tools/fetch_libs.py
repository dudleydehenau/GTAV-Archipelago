import io
import sys
import urllib.request
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parent.parent
sources = {
    "ScriptHookVDotNet3.dll": "https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases/download/v3.7.0-nightly.195/ScriptHookVDotNet-v3.7.0-nightly.195.zip",
    "LemonUI.SHVDN3.dll": "https://github.com/LemonUIbyLemon/LemonUI/releases/download/v2.2/LemonUI.zip",
}
mods = ["MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago"]

for name, url in sources.items():
    with urllib.request.urlopen(url) as response:
        archive = zipfile.ZipFile(io.BytesIO(response.read()))
    entries = [e for e in archive.namelist() if e.replace("\\", "/").split("/")[-1].lower() == name.lower()]
    if not entries:
        sys.exit(f"{name} not found in {url}")
    entry = sorted(entries, key=len)[0]
    data = archive.read(entry)
    for mod in mods:
        target = root / mod / "lib" / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    print(f"{name}: {entry} ({len(data)} bytes)")
