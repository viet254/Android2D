from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"D:\game\Sprites\Unity_Ready")
CONTACT_PATH = Path("sprite_analysis/unity_ready_contact_sheet.jpg")
KEY_COLORS = {(255, 0, 255), (0, 255, 0), (0, 128, 128)}


def checkerboard(size: tuple[int, int], cell: int = 10) -> Image.Image:
    canvas = Image.new("RGB", size, (222, 222, 222))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(174, 174, 174))
    return canvas


def has_transparent_border(image: Image.Image) -> bool:
    alpha = image.getchannel("A")
    width, height = image.size
    top = alpha.crop((0, 0, width, 1)).getextrema()[1]
    bottom = alpha.crop((0, height - 1, width, height)).getextrema()[1]
    left = alpha.crop((0, 0, 1, height)).getextrema()[1]
    right = alpha.crop((width - 1, 0, width, height)).getextrema()[1]
    return max(top, bottom, left, right) == 0


def opaque_key_pixels(image: Image.Image) -> int:
    count = 0
    for red, green, blue, alpha in image.convert("RGBA").getdata():
        if alpha and (red, green, blue) in KEY_COLORS:
            count += 1
    return count


def main() -> None:
    cleaned = sorted((ROOT / "01_Cleaned_Sheets").glob("*.png"))
    actions = sorted((ROOT / "02_Action_Strips").glob("*/*.png"))
    failures: list[str] = []
    samples: list[Path] = []

    for path in cleaned + actions:
        try:
            with Image.open(path) as source:
                source.verify()
            with Image.open(path) as source:
                image = source.convert("RGBA")
                if image.getchannel("A").getextrema()[0] != 0:
                    failures.append(f"No transparent pixels: {path}")
                if opaque_key_pixels(image):
                    failures.append(f"Opaque key color remains: {path}")
                if path in actions and not has_transparent_border(image):
                    failures.append(f"Action has opaque border: {path}")
        except Exception as error:
            failures.append(f"Cannot open {path}: {error}")

    for directory in sorted((ROOT / "02_Action_Strips").iterdir()):
        if directory.is_dir():
            samples.extend(sorted(directory.glob("*.png"))[:3])

    tile_size = (360, 230)
    preview_size = (330, 180)
    columns = 3
    rows = (len(samples) + columns - 1) // columns
    contact = Image.new("RGB", (columns * tile_size[0], rows * tile_size[1]), "#20242a")
    draw = ImageDraw.Draw(contact)
    font = ImageFont.load_default()

    for index, path in enumerate(samples):
        with Image.open(path) as source:
            image = source.convert("RGBA")
            image.thumbnail(preview_size, Image.Resampling.NEAREST)
            preview = checkerboard(preview_size)
            x = (preview_size[0] - image.width) // 2
            y = (preview_size[1] - image.height) // 2
            preview.paste(image, (x, y), image)
        column, row = index % columns, index // columns
        origin_x, origin_y = column * tile_size[0], row * tile_size[1]
        contact.paste(preview, (origin_x + 15, origin_y + 10))
        label = f"{path.parent.name[:40]}\n{path.name}"
        draw.multiline_text((origin_x + 15, origin_y + 195), label, fill="white", font=font, spacing=2)

    CONTACT_PATH.parent.mkdir(parents=True, exist_ok=True)
    contact.save(CONTACT_PATH, quality=45, optimize=True)
    summary = {
        "cleaned_sheets": len(cleaned),
        "action_strips": len(actions),
        "sample_previews": len(samples),
        "failures": failures,
    }
    Path("sprite_analysis/verification.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    print(CONTACT_PATH.resolve())


if __name__ == "__main__":
    main()
