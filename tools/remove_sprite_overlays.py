from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(r"D:\game\Sprites\Unity_Ready")

TARGETS = {
    ROOT
    / "01_Cleaned_Sheets"
    / "PC _ Computer - Blasphemous 2 - Bosses - Faceless One, Chisel of Oblivion (Tutorial Boss).png": (
        1160,
        250,
        1420,
        390,
    ),
    ROOT
    / "02_Action_Strips"
    / "PC_Computer_Blasphemous_2_Bosses_Faceless_One_Chisel_of_Oblivion_Tutorial_Boss"
    / "action_001.png": (1168, 245, 1428, 385),
}


def main() -> None:
    for path, box in TARGETS.items():
        with Image.open(path) as source:
            image = source.convert("RGBA")
        ImageDraw.Draw(image).rectangle(box, fill=(0, 0, 0, 0))
        image.save(path, optimize=True)
        print(f"Removed credits overlay: {path}")


if __name__ == "__main__":
    main()
