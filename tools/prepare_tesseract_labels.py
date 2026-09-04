from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(r"D:\game\Sprites")
MANIFEST = ROOT / "Unity_Ready" / "manifest.json"
OUTPUT = Path("sprite_analysis/tesseract_labels")


def slug(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9]+", "_", value).strip("_")[:90]


def main() -> None:
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    OUTPUT.mkdir(parents=True)
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    index: list[dict[str, object]] = []
    for item in data["files"]:
        if item["mode"] != "green_panels":
            continue
        with Image.open(ROOT / item["source"]) as source:
            rgb = source.convert("RGB")
            for action in item["actions"]:
                rect = action["source_rect"]
                y = rect["y"]
                crop = rgb.crop((0, max(0, y - 13), min(rgb.width, 700), y))
                gray = Image.new("L", crop.size, 255)
                source_pixels = crop.load()
                output_pixels = gray.load()
                for py in range(crop.height):
                    for px in range(crop.width):
                        red, green, blue = source_pixels[px, py]
                        is_text = abs(red - green) <= 4 and abs(green - blue) <= 4 and red > 32
                        output_pixels[px, py] = 0 if is_text else 255
                bbox = ImageOps.invert(gray).getbbox()
                if bbox:
                    gray = gray.crop((max(0, bbox[0] - 2), 0, min(gray.width, bbox[2] + 2), gray.height))
                gray = ImageOps.expand(gray, border=16, fill=255)
                gray = gray.resize((gray.width * 8, gray.height * 8), Image.Resampling.NEAREST)
                filename = f"{slug(item['source'])}__{action['index']:03d}.png"
                path = OUTPUT / filename
                gray.save(path, optimize=True)
                index.append({"source": item["source"], "index": action["index"], "path": str(path.resolve())})
    (OUTPUT / "index.json").write_text(json.dumps(index, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Prepared {len(index)} OCR label crops")


if __name__ == "__main__":
    main()
