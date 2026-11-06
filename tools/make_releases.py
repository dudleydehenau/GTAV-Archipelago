import fnmatch
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parent.parent
configuration = sys.argv[1] if len(sys.argv) > 1 else "Release"
out = root / "releases"
out.mkdir(exist_ok=True)

mods = ["MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago"]
skip = ["ScriptHookVDotNet*.dll", "LemonUI*.dll", "MissionLauncher.dll", "GameEnvironment.dll", "GameTraps.dll",
        "GameCollectibles.dll", "*.pdb", "*.xml"]


def version_of(project: Path) -> str:
    match = re.search(r"<Version>([^<]+)</Version>", project.read_text(encoding="utf-8"))
    return match.group(1) if match else "0.0.0"


def keep(mod: str, name: str) -> bool:
    return name == f"{mod}.dll" or not any(fnmatch.fnmatch(name, pattern) for pattern in skip)


for mod in mods:
    build = root / mod / "bin" / configuration / "net48"
    if not build.is_dir():
        sys.exit(f"Build {mod} in {configuration} first ({build} is missing).")
    archive = out / f"{mod}-{version_of(root / mod / (mod + '.csproj'))}.zip"
    archive.unlink(missing_ok=True)
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
        for item in sorted(build.iterdir()):
            if item.is_file() and keep(mod, item.name):
                z.write(item, item.name)
        data = build / mod
        if data.is_dir():
            for item in sorted(data.rglob("*")):
                if item.is_file():
                    z.write(item, str(item.relative_to(build)).replace("\\", "/"))
    print(archive)

subprocess.run([sys.executable, str(root / "GameArchipelago" / "tools" / "build_apworld.py")], check=True)
world = root / "GameArchipelago" / "apworld" / "gtav.apworld"
if world.exists():
    shutil.copy2(world, out / world.name)
    print(out / world.name)

setup = root / "GTAVArchipelagoSetup" / "bin" / configuration / "net48"
if setup.is_dir():
    archive = out / f"GTAVArchipelagoSetup-{version_of(root / 'GTAVArchipelagoSetup' / 'GTAVArchipelagoSetup.csproj')}.zip"
    archive.unlink(missing_ok=True)
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
        for name in ("GTAVArchipelagoSetup.exe", "sources.json"):
            z.write(setup / name, name)
    print(archive)
