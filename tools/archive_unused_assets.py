"""Archive unreferenced files from the partially used packs after asset_deps.py.

Only asset types covered by the GUID graph are candidates. Script/source and
shader subgraphs stay in place for the Unity owner's rebuild verification.
"""

from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
ARCHIVE = ROOT.parent / "catme-asset-archive" / "2026-10-04"
PACKS = (
    "JTS_Shrine_Torii_Gates", "PolyOne", "TileableBricksWall",
    "3D set of stylized nature - GHIBLI style", "AI cartoon butterflies",
    "ithappy", "Free",
)
EXTENSIONS = {
    ".fbx", ".prefab", ".anim", ".psd", ".mat", ".png", ".tga",
    ".controller", ".exr", ".asset", ".pdf", ".txt",
}


def main():
    used = set((ROOT / "tools/asset_used.txt").read_text().splitlines())
    moved = []
    for pack in PACKS:
        for path in sorted((ASSETS / pack).rglob("*")):
            if not path.is_file() or path.suffix.lower() not in EXTENSIONS:
                continue
            relative = path.relative_to(ROOT)
            if relative.as_posix() in used:
                continue
            # Catch literal references that a GUID-only graph cannot see.
            match = subprocess.run(
                ["rg", "-l", "-F", path.name, "Assets/CatMe", "ProjectSettings"],
                cwd=ROOT, capture_output=True, text=True, check=False,
            )
            if match.stdout.strip():
                continue
            destination = ARCHIVE / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(path, destination)
            meta = Path(str(path) + ".meta")
            if meta.exists():
                shutil.move(meta, Path(str(destination) + ".meta"))
            moved.append(relative.as_posix())
    (ARCHIVE / "STEP2_PATHS.txt").write_text("\n".join(moved) + "\n")
    print(f"Archived {len(moved)} assets and matching .meta files")
    print(f"Manifest: {ARCHIVE / 'STEP2_PATHS.txt'}")


if __name__ == "__main__":
    main()
