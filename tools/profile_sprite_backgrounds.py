from __future__ import annotations

from collections import Counter
from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\game\Sprites")
KEYS = {
    (255, 0, 255): "magenta",
    (255, 0, 136): "pink",
    (0, 255, 0): "green",
    (18, 255, 0): "bright-green",
    (0, 128, 128): "teal",
    (0, 0, 0): "black",
    (255, 255, 255): "white",
}


def main() -> None:
    for path in sorted(ROOT.glob("*.png"), key=lambda item: item.name.casefold()):
        with Image.open(path) as source:
            image = source.convert("RGBA")
            rgb = image.convert("RGB")
            total = image.width * image.height
            colors = Counter(rgb.getdata()).most_common(8)
            alpha_zero = sum(1 for value in image.getchannel("A").getdata() if value == 0)
            print(f"\n{path.name}\n  {image.width}x{image.height}; transparent={alpha_zero / total:.1%}")
            print("  top:", ", ".join(f"{color}={count / total:.1%}" for color, count in colors))
            counts = Counter(rgb.getdata())
            present = [f"{label}={counts[color] / total:.1%}" for color, label in KEYS.items() if counts[color]]
            print("  keys:", ", ".join(present))


if __name__ == "__main__":
    main()
