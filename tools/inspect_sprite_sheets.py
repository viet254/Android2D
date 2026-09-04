from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SOURCE_DIR = Path(r"D:\game\Sprites")
OUTPUT_DIR = Path("sprite_analysis")
THUMB_SIZE = (420, 300)


def edge_colors(image: Image.Image) -> list[tuple[tuple[int, int, int, int], int]]:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    colors: Counter[tuple[int, int, int, int]] = Counter()
    for x in range(width):
        colors[pixels[x, 0]] += 1
        colors[pixels[x, height - 1]] += 1
    for y in range(1, height - 1):
        colors[pixels[0, y]] += 1
        colors[pixels[width - 1, y]] += 1
    return colors.most_common(6)


def checkerboard(size: tuple[int, int], cell: int = 12) -> Image.Image:
    canvas = Image.new("RGB", size, (224, 224, 224))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(184, 184, 184))
    return canvas


def make_preview(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    scale = min(THUMB_SIZE[0] / rgba.width, THUMB_SIZE[1] / rgba.height)
    size = (max(1, round(rgba.width * scale)), max(1, round(rgba.height * scale)))
    resized = rgba.resize(size, Image.Resampling.NEAREST)
    background = checkerboard(THUMB_SIZE)
    left = (THUMB_SIZE[0] - size[0]) // 2
    top = (THUMB_SIZE[1] - size[1]) // 2
    background.paste(resized, (left, top), resized)
    return background


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    files = sorted(SOURCE_DIR.glob("*.png"), key=lambda path: path.name.casefold())
    report = []
    font = ImageFont.load_default()
    cell_width, cell_height = 460, 360
    columns = 3
    rows = (len(files) + columns - 1) // columns
    contact = Image.new("RGB", (columns * cell_width, rows * cell_height), "#20242a")
    draw = ImageDraw.Draw(contact)

    for index, path in enumerate(files):
        with Image.open(path) as image:
            rgba = image.convert("RGBA")
            alpha = rgba.getchannel("A")
            alpha_extrema = alpha.getextrema()
            transparent_pixels = sum(1 for value in alpha.getdata() if value == 0)
            partial_alpha_pixels = sum(1 for value in alpha.getdata() if 0 < value < 255)
            colors = edge_colors(rgba)
            item = {
                "file": path.name,
                "width": rgba.width,
                "height": rgba.height,
                "mode": image.mode,
                "alpha_min": alpha_extrema[0],
                "alpha_max": alpha_extrema[1],
                "transparent_pixels": transparent_pixels,
                "partial_alpha_pixels": partial_alpha_pixels,
                "edge_colors": [
                    {"rgba": list(color), "count": count} for color, count in colors
                ],
            }
            report.append(item)

            col, row = index % columns, index // columns
            origin_x, origin_y = col * cell_width, row * cell_height
            preview = make_preview(rgba)
            contact.paste(preview, (origin_x + 20, origin_y + 20))
            label = f"{index + 1:02}. {path.name}\n{rgba.width}x{rgba.height} | {image.mode} | alpha {alpha_extrema}"
            draw.multiline_text((origin_x + 20, origin_y + 325), label, fill="white", font=font, spacing=3)

    (OUTPUT_DIR / "sprite_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    contact.save(OUTPUT_DIR / "contact_sheet.png")
    print(f"Analyzed {len(files)} PNG files")
    print((OUTPUT_DIR / "sprite_report.json").resolve())
    print((OUTPUT_DIR / "contact_sheet.png").resolve())


if __name__ == "__main__":
    main()
