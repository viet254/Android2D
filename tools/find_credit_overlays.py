from __future__ import annotations

import json
import re
from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\game\Sprites\Unity_Ready\02_Action_Strips")


def warm_mask_bytes(image: Image.Image) -> bytes:
    output = bytearray(image.width * image.height)
    for index, (red, green, blue, alpha) in enumerate(image.convert("RGBA").getdata()):
        if alpha and red == 255 and blue == 0 and 100 <= green <= 220:
            output[index] = 255
    return bytes(output)


def candidates(image: Image.Image) -> list[dict[str, int | float]]:
    width, height = image.size
    raw = warm_mask_bytes(image)
    pattern = re.compile(rb"\xff{8,}")
    groups: list[dict[str, int]] = []
    active: list[int] = []
    for y in range(height):
        row = raw[y * width : (y + 1) * width]
        runs = [match.span() for match in pattern.finditer(row)]
        next_active: list[int] = []
        for start, end in runs:
            overlaps = [
                index
                for index in active
                if groups[index]["last_y"] == y - 1
                and groups[index]["min_x"] < end
                and start < groups[index]["max_x"]
            ]
            if overlaps:
                index = overlaps[0]
                group = groups[index]
                group["min_x"] = min(group["min_x"], start)
                group["max_x"] = max(group["max_x"], end)
                group["last_y"] = y
                group["pixels"] += end - start
            else:
                index = len(groups)
                groups.append(
                    {"min_x": start, "max_x": end, "min_y": y, "last_y": y, "pixels": end - start}
                )
            next_active.append(index)
        active = list(dict.fromkeys(next_active))

    result: list[dict[str, int | float]] = []
    for group in groups:
        box_width = group["max_x"] - group["min_x"]
        box_height = group["last_y"] - group["min_y"] + 1
        fill = group["pixels"] / (box_width * box_height)
        if box_width >= 40 and box_height >= 8 and group["pixels"] >= 300 and fill >= 0.2:
            result.append(
                {
                    "x": group["min_x"],
                    "y": group["min_y"],
                    "width": box_width,
                    "height": box_height,
                    "pixels": group["pixels"],
                    "fill": round(fill, 3),
                }
            )
    return result


def main() -> None:
    found: list[dict[str, object]] = []
    for path in sorted(ROOT.glob("*/*.png")):
        with Image.open(path) as source:
            image = source.convert("RGBA")
            matches = candidates(image)
        if matches:
            found.append({"file": str(path), "candidates": matches})
            print(path)
            print(matches)
    Path("sprite_analysis/credit_overlay_candidates.json").write_text(
        json.dumps(found, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"candidate files: {len(found)}")


if __name__ == "__main__":
    main()
