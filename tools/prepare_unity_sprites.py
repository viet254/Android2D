from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


SOURCE_DIR = Path(r"D:\game\Sprites")
OUTPUT_ROOT = SOURCE_DIR / "Unity_Ready"
CLEANED_DIR = OUTPUT_ROOT / "01_Cleaned_Sheets"
ACTIONS_DIR = OUTPUT_ROOT / "02_Action_Strips"
PADDING = 8

MAGENTA = (255, 0, 255)
GREEN = (0, 255, 0)
TEAL = (0, 128, 128)

SKIP_MARKERS = ("Main Menu", "Tileset", "Screens")


def slugify(value: str) -> str:
    value = value.rsplit(".", 1)[0]
    value = re.sub(r"[^A-Za-z0-9]+", "_", value).strip("_")
    return value[:100]


def exact_color_mask(image: Image.Image, color: tuple[int, int, int]) -> Image.Image:
    red, green, blue = image.convert("RGB").split()
    red_mask = red.point([255 if value == color[0] else 0 for value in range(256)])
    green_mask = green.point([255 if value == color[1] else 0 for value in range(256)])
    blue_mask = blue.point([255 if value == color[2] else 0 for value in range(256)])
    return ImageChops.multiply(ImageChops.multiply(red_mask, green_mask), blue_mask)


def flood_outer_black(image: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    draw = ImageDraw.Draw(rgb)
    width, height = rgb.size
    black = (0, 0, 0)
    marker = (1, 2, 3)
    border_points = (
        [(x, 0) for x in range(width)]
        + [(x, height - 1) for x in range(width)]
        + [(0, y) for y in range(height)]
        + [(width - 1, y) for y in range(height)]
    )
    pixels = rgb.load()
    for point in border_points:
        if pixels[point] == black:
            ImageDraw.floodfill(rgb, point, marker, thresh=0)
    return exact_color_mask(rgb, marker)


def key_to_alpha(
    source: Image.Image,
    keys: list[tuple[int, int, int]],
    remove_outer_black: bool = False,
) -> Image.Image:
    image = source.convert("RGBA")
    alpha = image.getchannel("A")
    for key in keys:
        alpha = ImageChops.subtract(alpha, exact_color_mask(image, key))
    if remove_outer_black:
        alpha = ImageChops.subtract(alpha, flood_outer_black(image))
    image.putalpha(alpha)
    return image


def true_runs(values: tuple[int, ...] | list[int]) -> list[tuple[int, int]]:
    runs: list[tuple[int, int]] = []
    start: int | None = None
    for index, value in enumerate(values):
        if value and start is None:
            start = index
        elif not value and start is not None:
            runs.append((start, index))
            start = None
    if start is not None:
        runs.append((start, len(values)))
    return runs


def green_action_regions(source: Image.Image) -> list[tuple[int, int, int, int]]:
    mask = exact_color_mask(source, GREEN)
    _, y_projection = mask.getprojection()
    regions: list[tuple[int, int, int, int]] = []
    for top, bottom in true_runs(y_projection):
        if bottom - top < 8:
            continue
        strip = mask.crop((0, top, mask.width, bottom))
        x_projection, _ = strip.getprojection()
        for left, right in true_runs(x_projection):
            if right - left < 8 or (right - left) * (bottom - top) < 256:
                continue
            regions.append((left, top, right, bottom))
    return regions


def alpha_row_regions(image: Image.Image) -> list[tuple[int, int, int, int]]:
    alpha = image.getchannel("A").point([0] + [255] * 255)
    _, y_projection = alpha.getprojection()
    regions: list[tuple[int, int, int, int]] = []
    for top, bottom in true_runs(y_projection):
        if bottom - top < 8:
            continue
        bbox = image.crop((0, top, image.width, bottom)).getbbox()
        if bbox is None:
            continue
        left, local_top, right, local_bottom = bbox
        if right - left < 8:
            continue
        regions.append((left, top + local_top, right, top + local_bottom))
    return regions


def padded_crop(image: Image.Image, box: tuple[int, int, int, int]) -> Image.Image | None:
    crop = image.crop(box)
    bbox = crop.getbbox()
    if bbox is None:
        return None
    crop = crop.crop(bbox)
    output = Image.new("RGBA", (crop.width + PADDING * 2, crop.height + PADDING * 2), (0, 0, 0, 0))
    output.alpha_composite(crop, (PADDING, PADDING))
    return output


def processing_plan(path: Path) -> tuple[list[tuple[int, int, int]], bool, str]:
    name = path.name
    if name.startswith("Arcade - Red Earth"):
        return [MAGENTA], True, "cleaned_sheet"
    if "Charred Skull" in name or "Dummy Variant" in name:
        return [MAGENTA], False, "alpha_rows"
    return [GREEN, TEAL], False, "green_panels"


def write_readme(manifest: list[dict[str, object]], skipped: list[str]) -> None:
    green_count = sum(1 for item in manifest if item["mode"] == "green_panels")
    alpha_row_count = sum(1 for item in manifest if item["mode"] == "alpha_rows")
    text = f"""# Sprite đã chuẩn hóa cho Unity

Thư mục này được tạo tự động, không ghi đè ảnh gốc.

## Đã xử lý

- {len(manifest)} sprite sheet animation được đổi sang PNG RGBA nền trong suốt.
- {green_count} sheet nền xanh lá/teal được tách thành từng dải hành động theo thứ tự từ trên xuống, trái sang phải.
- {alpha_row_count} sheet nền magenta toàn trang được tách thêm theo các hàng nội dung có thể nhận diện an toàn.
- Mỗi dải hành động có viền trong suốt {PADDING}px để tránh dính với dải khác.
- Các sheet Red Earth giữ nguyên bố cục nhưng đã bỏ magenta và phần nền đen nối với mép ảnh. Bố cục của chúng không chứa nhãn hành động đáng tin cậy nên không tự gom nhóm để tránh xáo trộn frame.

## Cách import trong Unity

1. Chép dải hành động cần dùng từ `02_Action_Strips` vào một thư mục con trong `Assets`.
2. Trong Inspector đặt `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`.
3. Với pixel art: `Filter Mode = Point (no filter)`, `Compression = None`, tắt mipmap và bật `Alpha Is Transparency`.
4. Mở `Sprite Editor` > `Slice` > `Automatic`, pivot nên dùng `Bottom Center`; Apply.
5. Chọn các frame từ trái sang phải trong đúng thư mục hành động rồi kéo vào Scene/Animation để tạo clip.

Nếu một frame có vũ khí hoặc hiệu ứng rời thân và Automatic tách thành nhiều sprite, hãy vẽ một rect thủ công bao toàn bộ frame đó. Không nên xóa hiệu ứng nhỏ bằng thuật toán vì sẽ làm sai animation gốc.

## Ảnh không xử lý như animation

Các file sau được giữ nguyên vì là menu, màn hình hoặc tileset:

{chr(10).join(f'- {name}' for name in skipped)}

## Lưu ý

Tên `action_001`, `action_002`, ... giữ đúng thứ tự vị trí trên sheet nguồn. Hãy xem `manifest.json` để tra tọa độ gốc. Cần kiểm tra quyền sử dụng của asset trước khi phát hành game.
"""
    (OUTPUT_ROOT / "README_VI.md").write_text(text, encoding="utf-8")


def main() -> None:
    if OUTPUT_ROOT.exists():
        shutil.rmtree(OUTPUT_ROOT)
    CLEANED_DIR.mkdir(parents=True, exist_ok=True)
    ACTIONS_DIR.mkdir(parents=True, exist_ok=True)

    manifest: list[dict[str, object]] = []
    skipped: list[str] = []
    source_files = sorted(SOURCE_DIR.glob("*.png"), key=lambda item: item.name.casefold())

    for path in source_files:
        if any(marker in path.name for marker in SKIP_MARKERS):
            skipped.append(path.name)
            continue

        keys, remove_outer_black, mode = processing_plan(path)
        with Image.open(path) as source:
            source.load()
            cleaned = key_to_alpha(source, keys, remove_outer_black)
            cleaned_path = CLEANED_DIR / path.name
            cleaned.save(cleaned_path, optimize=True)

            if mode == "green_panels":
                regions = green_action_regions(source)
            elif mode == "alpha_rows":
                regions = alpha_row_regions(cleaned)
            else:
                regions = []

            action_dir = ACTIONS_DIR / slugify(path.name)
            action_records: list[dict[str, object]] = []
            for index, box in enumerate(regions, 1):
                action = padded_crop(cleaned, box)
                if action is None:
                    continue
                action_dir.mkdir(parents=True, exist_ok=True)
                action_name = f"action_{index:03d}.png"
                action_path = action_dir / action_name
                action.save(action_path, optimize=True)
                action_records.append(
                    {
                        "index": index,
                        "file": str(action_path.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
                        "source_rect": {
                            "x": box[0],
                            "y": box[1],
                            "width": box[2] - box[0],
                            "height": box[3] - box[1],
                        },
                        "output_width": action.width,
                        "output_height": action.height,
                    }
                )

        record = {
            "source": path.name,
            "cleaned": str(cleaned_path.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
            "removed_colors": [list(color) for color in keys],
            "removed_outer_black": remove_outer_black,
            "mode": mode,
            "action_count": len(action_records),
            "actions": action_records,
        }
        manifest.append(record)
        print(f"{path.name}: cleaned; actions={len(action_records)}")

    (OUTPUT_ROOT / "manifest.json").write_text(
        json.dumps({"files": manifest, "skipped": skipped}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    write_readme(manifest, skipped)
    print(f"Output: {OUTPUT_ROOT}")


if __name__ == "__main__":
    main()
