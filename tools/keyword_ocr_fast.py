from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


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
    "defend", "defense", "counter", "move", "moving", "prepare", "hold", "release", "step",
]


def source_mask(image: Image.Image, left: int, label_y: int, width: int = 520) -> np.ndarray:
    top = max(0, label_y - 13)
    crop = np.asarray(image.crop((left, top, min(image.width, left + width), label_y)).convert("RGB"))
    close_gray = (np.abs(crop[:, :, 0].astype(int) - crop[:, :, 1]) <= 3) & (
        np.abs(crop[:, :, 1].astype(int) - crop[:, :, 2]) <= 3
    )
    return (close_gray & (crop[:, :, 0] > 32)).astype(np.float32)


def templates() -> dict[str, list[np.ndarray]]:
    result: dict[str, list[np.ndarray]] = {}
    for word in WORDS:
        variants: list[np.ndarray] = []
        for font_path in FONT_PATHS:
            font = ImageFont.truetype(str(font_path), 10)
            for case in (word, word.capitalize(), word.upper()):
                for y_offset in (0, 1, 2):
                    probe = Image.new("L", (200, 13))
                    ImageDraw.Draw(probe).text((0, y_offset), case, fill=255, font=font)
                    bbox = probe.getbbox()
                    if not bbox:
                        continue
                    array = (np.asarray(probe.crop((bbox[0], 0, bbox[2], 13))) > 32).astype(np.float32)
                    variants.append(array)
        result[word] = variants
    return result


def best_score(source: np.ndarray, variants: list[np.ndarray]) -> float:
    best = 0.0
    for target in variants:
        width = target.shape[1]
        if width > source.shape[1]:
            continue
        windows = np.lib.stride_tricks.sliding_window_view(source, (13, width))[0]
        intersection = (windows * target).sum(axis=(1, 2))
        denominator = windows.sum(axis=(1, 2)) + target.sum()
        scores = np.divide(2 * intersection, denominator, out=np.zeros_like(intersection), where=denominator > 0)
        best = max(best, float(scores.max(initial=0.0)))
    return best


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("filter")
    args = parser.parse_args()
    model = templates()
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    report: list[dict[str, object]] = []
    for item in data["files"]:
        if args.filter.casefold() not in item["source"].casefold():
            continue
        print(f"SOURCE: {item['source']}")
        item_scores: list[dict[str, object]] = []
        with Image.open(ROOT / item["source"]) as source:
            for action in item["actions"]:
                rect = action["source_rect"]
                mask = source_mask(source, max(0, rect["x"] - 2), rect["y"])
                scores = sorted(((best_score(mask, variants), word) for word, variants in model.items()), reverse=True)[:8]
                print(f"{action['index']:03d}: " + ", ".join(f"{word}={score:.3f}" for score, word in scores))
                item_scores.append({"index": action["index"], "scores": [{"word": w, "score": round(s, 4)} for s, w in scores]})
        report.append({"source": item["source"], "actions": item_scores})
    output = Path("sprite_analysis") / f"keyword_ocr_{args.filter.replace(' ', '_')}.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
