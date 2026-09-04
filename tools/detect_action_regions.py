from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops


SOURCE_DIR = Path(r"D:\game\Sprites")
REPORT_PATH = Path("sprite_analysis/action_regions.json")
MAGENTA = (255, 0, 255)
GREEN = (0, 255, 0)


@dataclass
class Region:
    color: tuple[int, int, int]
    x: int
    y: int
    width: int
    height: int
    pixels: int
    fill: float


class UnionFind:
    def __init__(self) -> None:
        self.parent: list[int] = []

    def add(self) -> int:
        node = len(self.parent)
        self.parent.append(node)
        return node

    def find(self, node: int) -> int:
        parent = self.parent
        while parent[node] != node:
            parent[node] = parent[parent[node]]
            node = parent[node]
        return node

    def union(self, first: int, second: int) -> None:
        first_root = self.find(first)
        second_root = self.find(second)
        if first_root != second_root:
            self.parent[second_root] = first_root


def exact_color_mask(image: Image.Image, color: tuple[int, int, int]) -> Image.Image:
    red, green, blue = image.convert("RGB").split()
    red_mask = red.point([255 if value == color[0] else 0 for value in range(256)])
    green_mask = green.point([255 if value == color[1] else 0 for value in range(256)])
    blue_mask = blue.point([255 if value == color[2] else 0 for value in range(256)])
    return ImageChops.multiply(ImageChops.multiply(red_mask, green_mask), blue_mask)


def connected_regions(mask: Image.Image, color: tuple[int, int, int]) -> list[Region]:
    width, height = mask.size
    raw = mask.tobytes()
    run_pattern = re.compile(rb"\xff+")
    union_find = UnionFind()
    runs: list[tuple[int, int, int, int]] = []
    previous: list[tuple[int, int, int]] = []

    for y in range(height):
        row = raw[y * width : (y + 1) * width]
        current: list[tuple[int, int, int]] = []
        for match in run_pattern.finditer(row):
            start, end = match.span()
            node = union_find.add()
            current.append((start, end, node))
            runs.append((y, start, end, node))

        previous_index = 0
        for start, end, node in current:
            while previous_index < len(previous) and previous[previous_index][1] <= start:
                previous_index += 1
            scan = previous_index
            while scan < len(previous) and previous[scan][0] < end:
                union_find.union(node, previous[scan][2])
                scan += 1
        previous = current

    stats: dict[int, list[int]] = {}
    for y, start, end, node in runs:
        root = union_find.find(node)
        if root not in stats:
            stats[root] = [start, y, end, y + 1, end - start]
        else:
            item = stats[root]
            item[0] = min(item[0], start)
            item[1] = min(item[1], y)
            item[2] = max(item[2], end)
            item[3] = max(item[3], y + 1)
            item[4] += end - start

    regions: list[Region] = []
    for min_x, min_y, max_x, max_y, pixels in stats.values():
        region_width = max_x - min_x
        region_height = max_y - min_y
        area = region_width * region_height
        if region_width < 8 or region_height < 8 or pixels < 100:
            continue
        fill = pixels / area
        if fill < 0.08:
            continue
        regions.append(
            Region(color, min_x, min_y, region_width, region_height, pixels, fill)
        )
    regions.sort(key=lambda region: (region.y, region.x))
    return regions


def background_color(path: Path) -> tuple[int, int, int] | None:
    if "Main Menu" in path.name or "Tileset" in path.name or "Screens" in path.name:
        return None
    if path.name.startswith("Arcade - Red Earth") or "Charred Skull" in path.name or "Dummy Variant" in path.name:
        return MAGENTA
    return GREEN


def main() -> None:
    report: list[dict[str, object]] = []
    for path in sorted(SOURCE_DIR.glob("*.png"), key=lambda item: item.name.casefold()):
        color = background_color(path)
        if color is None:
            continue
        with Image.open(path) as image:
            regions = connected_regions(exact_color_mask(image, color), color)
        report.append(
            {
                "file": path.name,
                "background": list(color),
                "regions": [
                    {
                        "x": region.x,
                        "y": region.y,
                        "width": region.width,
                        "height": region.height,
                        "pixels": region.pixels,
                        "fill": round(region.fill, 4),
                    }
                    for region in regions
                ],
            }
        )
        print(f"{path.name}: {len(regions)} action candidates")

    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(REPORT_PATH.resolve())


if __name__ == "__main__":
    main()
