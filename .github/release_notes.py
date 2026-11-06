import re
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
version = sys.argv[1] if len(sys.argv) > 1 else ""
out = root / "releases"
out.mkdir(exist_ok=True)


def latest(changelog: Path) -> str:
    if not changelog.exists():
        return ""
    text = changelog.read_text(encoding="utf-8")
    parts = re.split(r"^## ", text, flags=re.MULTILINE)
    if len(parts) < 2:
        return ""
    head, _, body = parts[1].partition("\n")
    return f"### {head.strip()}\n{body.strip()}\n"


def version_of(project: Path) -> str:
    match = re.search(r"<Version>([^<]+)</Version>", project.read_text(encoding="utf-8"))
    return match.group(1) if match else "?"


mods = ["GameArchipelago", "GameEnvironment", "GameCollectibles", "GameTraps", "MissionLauncher"]
lines = [f"# GTA V Archipelago {version} - mods", "", "The mod files used by the setup. To install, take `GTAVArchipelagoSetup.exe` and `gtav.apworld` from the latest release instead.", ""]
for mod in mods:
    lines.append(f"## {mod} {version_of(root / mod / (mod + '.csproj'))}")
    lines.append("")
    lines.append(latest(root / mod / "CHANGELOG.md"))
(out / "notes-mods.md").write_text("\n".join(lines), encoding="utf-8")

setup = root / "GTAVArchipelagoSetup"
setup_lines = [
    f"# GTA V Archipelago Setup {version_of(setup / 'GTAVArchipelagoSetup.csproj')}",
    "",
    "Download `GTAVArchipelagoSetup.exe`, run it and pick your GTA V folder: it installs ScriptHookVDotNet, LemonUI and every mod.",
    "Put `gtav.apworld` in Archipelago to generate a game.",
    "",
    latest(setup / "CHANGELOG.md"),
]
(out / "notes-setup.md").write_text("\n".join(setup_lines), encoding="utf-8")
print("notes written for", version)
