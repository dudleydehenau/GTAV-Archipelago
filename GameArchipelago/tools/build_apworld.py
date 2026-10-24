import json
import pathlib
import zipfile

root = pathlib.Path(__file__).resolve().parent.parent
world = root / "apworld" / "gtav"
manifest = json.loads((world / "archipelago.json").read_text(encoding="utf-8"))
manifest["version"] = 7
manifest["compatible_version"] = 7
output = root / "apworld" / "gtav.apworld"
with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(world.rglob("*")):
        if path.is_dir() or "__pycache__" in path.parts or path.name == "archipelago.json":
            continue
        archive.write(path, "gtav/" + path.relative_to(world).as_posix())
    archive.writestr("gtav/archipelago.json", json.dumps(manifest, indent=2))
print("wrote", output)
