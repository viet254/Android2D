from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\game\Sprites")
MANIFEST = ROOT / "Unity_Ready" / "manifest.json"


def label_mask(image: Image.Image, left: int, label_y: int, width: int = 420) -> list[list[bool]]:
    top = max(0, label_y - 13)
    crop = image.crop((left, top, min(image.width, left + width), label_y)).convert("RGB")
    mask: list[list[bool]] = []
    for y in range(crop.height):
        row: list[bool] = []
        for x in range(crop.width):
            red, green, blue = crop.getpixel((x, y))
            row.append(abs(red - green) <= 3 and abs(green - blue) <= 3 and red > 32)
        mask.append(row)
    return mask


def print_mask(mask: list[list[bool]]) -> None:
    columns = len(mask[0]) if mask else 0
    used = [x for x in range(columns) if any(row[x] for row in mask)]
    if not used:
        print("  [no readable label]")
        return
    left, right = min(used), max(used) + 1
    for row in mask:
        print("".join("#" if value else " " for value in row[left:right]).rstrip())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("filter")
    args = parser.parse_args()
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    for item in data["files"]:
        if args.filter.casefold() not in item["source"].casefold():
            continue
        print(f"SOURCE: {item['source']}")
        with Image.open(ROOT / item["source"]) as source:
            for action in item["actions"]:
                rect = action["source_rect"]
                print(f"\nACTION {action['index']:03d}")
                print_mask(label_mask(source, max(0, rect["x"] - 2), rect["y"]))


if __name__ == "__main__":
    main()
