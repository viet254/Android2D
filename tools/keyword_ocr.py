from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat


ROOT = Path(r"D:\game\Sprites")
MANIFEST = ROOT / "Unity_Ready" / "manifest.json"
FONT_PATHS = [Path(r"C:\Windows\Fonts\micross.ttf"), Path(r"C:\Windows\Fonts\arial.ttf")]
WORDS = [
    "idle", "walk", "run", "turn", "jump", "fall", "land", "death", "dead", "hurt", "hit",
    "attack", "charge", "start", "loop", "end", "flapping", "fly", "flying", "dash", "roll",
    "block", "guard", "parry", "cast", "spawn", "appear", "disappear", "intro", "outro", "wake",
    "throw", "projectile", "shockwave", "stomp", "slash", "swing", "stab", "shoot", "impact",
    "recover", "recovery", "anticipation", "air", "ground", "up", "down", "left", "right",
    "back", "front", "mask", "shield", "sword", "weapon", "effect", "vfx", "particles", "trail",
    "break", "broken", "explode", "explosion", "teleport", "summon", "taunt", "roar", "grab",
    "phase", "transition", "special", "combo", "spin", "spinning", "rise", "drop", "kick", "punch",
]


def source_mask(image: Image.Image, left: int, label_y: int, width: int = 500) -> Image.Image:
    top = max(0, label_y - 13)
    crop = image.crop((left, top, min(image.width, left + width), label_y)).convert("RGB")
    mask = Image.new("L", crop.size)
    src = crop.load()
    dst = mask.load()
    for y in range(crop.height):
        for x in range(crop.width):
            red, green, blue = src[x, y]
            dst[x, y] = 255 if abs(red - green) <= 3 and abs(green - blue) <= 3 and red > 32 else 0
    return mask


def template(word: str, font_path: Path, y_offset: int) -> Image.Image:
    font = ImageFont.truetype(str(font_path), 10)
    probe = Image.new("L", (200, 13))
    draw = ImageDraw.Draw(probe)
    draw.text((0, y_offset), word, fill=255, font=font)
    bbox = probe.getbbox()
    return probe.crop((bbox[0], 0, bbox[2], 13)) if bbox else Image.new("L", (1, 13))


def best_score(source: Image.Image, word: str) -> float:
    best = 0.0
    for font_path in FONT_PATHS:
        for case in (word, word.capitalize(), word.upper()):
            for y_offset in (0, 1, 2):
                target = template(case, font_path, y_offset).point(lambda value: 255 if value > 32 else 0)
                target_sum = ImageStat.Stat(target).sum[0] / 255
                if target.width > source.width or not target_sum:
                    continue
                for x in range(source.width - target.width + 1):
                    sample = source.crop((x, 0, x + target.width, 13))
                    sample_sum = ImageStat.Stat(sample).sum[0] / 255
                    intersection = ImageStat.Stat(ImageChops.multiply(sample, target)).sum[0] / 255
                    score = 2 * intersection / (sample_sum + target_sum) if sample_sum + target_sum else 0.0
                    if score > best:
                        best = score
    return best


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
                mask = source_mask(source, max(0, rect["x"] - 2), rect["y"])
                scores = sorted(((best_score(mask, word), word) for word in WORDS), reverse=True)[:6]
                print(f"{action['index']:03d}: " + ", ".join(f"{word}={score:.3f}" for score, word in scores))


if __name__ == "__main__":
    main()
