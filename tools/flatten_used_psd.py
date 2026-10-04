"""Flatten used torii and butterfly PSDs, retaining Unity texture GUIDs."""

from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT.parent / "catme-asset-archive" / "2026-10-04"
used = (ROOT / "tools/asset_used.txt").read_text().splitlines()
paths = [Path(path) for path in used if path.lower().endswith(".psd")
         and ("JTS_Shrine_Torii_Gates" in path or "AI cartoon butterflies" in path)]
converted = []
for relative in paths:
    source = ROOT / relative
    output = source.with_suffix(".png")
    if output.exists():
        raise RuntimeError(f"Output already exists: {output}")
    limit = "2048" if "JTS_Shrine_Torii_Gates" in str(relative) else "1024"
    subprocess.run(["sips", "-s", "format", "png", "--resampleHeightWidthMax", limit,
                    str(source), "--out", str(output)], check=True, capture_output=True)
    if not output.is_file() or output.stat().st_size == 0:
        raise RuntimeError(f"Conversion failed: {relative}")
    archived = ARCHIVE / relative
    archived.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(source, archived)
    shutil.move(Path(str(source) + ".meta"), Path(str(archived) + ".meta"))
    shutil.copy2(Path(str(archived) + ".meta"), Path(str(output) + ".meta"))
    converted.append(f"{relative.as_posix()} -> {output.relative_to(ROOT).as_posix()}")
(ARCHIVE / "STEP2_CONVERTED.txt").write_text("\n".join(converted) + "\n")
print(f"Flattened {len(converted)} PSD textures")
