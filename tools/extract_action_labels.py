from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SOURCE_ROOT = Path(r"D:\game\Sprites")
OUTPUT_ROOT = Path("sprite_analysis/action_labels")
MANIFEST = SOURCE_ROOT / "Unity_Ready" / "manifest.json"


def slugify(value: str) -> str:
    return "".join(char if char.isalnum() else "_" for char in value).strip("_")[:90]


def main() -> None:
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    font = ImageFont.load_default()

    for item in data["files"]:
        if not item["actions"]:
            continue
        source_path = SOURCE_ROOT / item["source"]
        with Image.open(source_path) as source:
            image = source.convert("RGB")
            width = 1500
            row_height = 72
            sheet = Image.new("RGB", (width, row_height * len(item["actions"])), "#20242a")
            draw = ImageDraw.Draw(sheet)
            for row, action in enumerate(item["actions"]):
                rect = action["source_rect"]
                left = max(0, rect["x"] - 2)
                top = max(0, rect["y"] - 14)
                right = min(image.width, left + 360)
                bottom = min(image.height, rect["y"] + 2)
                crop = image.crop((left, top, right, bottom)).resize(
                    ((right - left) * 4, (bottom - top) * 4), Image.Resampling.NEAREST
                )
                y = row * row_height
                sheet.paste(crop.crop((0, 0, min(crop.width, width - 120), min(crop.height, row_height))), (120, y))
                draw.text((8, y + 22), f"action_{action['index']:03d}", fill="white", font=font)

        output = OUTPUT_ROOT / f"{slugify(item['source'])}.png"
        sheet.save(output, optimize=True)
        print(output.resolve())


if __name__ == "__main__":
    main()
