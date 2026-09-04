from __future__ import annotations

import json
import shutil
from pathlib import Path


ROOT = Path(r"D:\game\Sprites\Unity_Ready")
MANIFEST = ROOT / "manifest.json"
BACKUP = ROOT / "manifest_before_action_rename.json"
REPORT = ROOT / "action_name_map.json"


NAMES: dict[str, list[str]] = {
    "PC _ Computer - Blasphemous - Enemies - Seraph - idel.png": [
        "idle",
    ],
    "PC _ Computer - Blasphemous - Enemies - Seraph.png": [
        "charge_attack_end",
        "charge_attack_loop",
        "charge_attack_start",
        "death",
        "flapping",
        "idle",
    ],
    "PC _ Computer - Blasphemous 2 - Bosses - Faceless One, Chisel of Oblivion (Tutorial Boss).png": [
        "charge",
        "death",
        "idle",
        "smear",
        "spinning_wheel_01",
        "spinning_wheel_02",
        "spinning_wheel_03",
        "throw_00",
        "throw_01",
        "throw_05",
        "throw_09",
        "throw_23",
        "throw_24",
        "throw_25",
        "throw_26",
        "throw_30",
        "wheel",
    ],
    "PC _ Computer - Blasphemous 2 - Bosses - Odon, of the Confraternity of Salt.png": [
        "idle",
        "jump_00",
        "jump_07",
        "jump_08",
        "jump_09",
        "jump_10_part_01",
        "jump_10_part_02",
        "jump_17",
        "light",
        "step_horizontal_slash",
        "stun",
        "summoning",
        "summoning_lance_vfx",
        "summoning_shield_vfx",
        "typhoon_vfx",
        "water_tide",
        "death_01",
        "lancers_part_01",
        "lancers_part_02",
        "turn",
        "intro_00",
        "intro_04",
        "intro_05",
        "intro_09",
        "walk",
    ],
    "PC _ Computer - Blasphemous 2 - DLC Bosses - Brother Asterion.png": [
        "attack_01_extra_00",
        "attack_01_extra_08",
        "attack_01_turn_around",
        "attack_01_turn_around_extra",
        "attack_01_updated",
        "attack_02",
        "attack_02_updated",
        "attack_03_projectile",
        "attack_03_updated",
        "attack_04",
        "attack_04_horizontal",
        "attack_04_vertical_anticipation_vfx",
        "attack_04_vertical",
        "attack_04_vfx",
        "attack_04_vfx_big",
        "attack_05_extra",
        "attack_05_updated_part_01",
        "attack_05_updated_part_02",
        "attack_06",
        "attack_07",
        "attack_05_vfx_head_00",
        "attack_05_vfx_head_09",
        "dust_big_vfx",
        "dust_loop_vfx",
        "dust_medium_vfx",
        "final_defeat",
        "final_defeat_idle_00",
        "final_defeat_idle_11",
        "idle",
        "levitate",
        "parry",
        "turn_around",
        "walk",
        "little_wave",
        "medium_wave",
        "projectile",
        "slash_projectile",
        "spikes_blades_01",
        "spikes_blades_02",
        "wave",
        "defeated_sequence_with_sword",
    ],
    "PC _ Computer - Blasphemous 2 - DLC Enemies - Shockwave.png": [
        "anticipation",
        "attack",
        "death",
        "idle",
        "recovery",
        "spawn",
    ],
    "PC _ Computer - Blasphemous 2 - Enemies - Chandelier Thrower.png": [
        "idle_part_01",
        "idle_part_02",
        "idle_part_03",
        "idle_part_04",
        "idle_part_05",
        "idle_part_06",
        "stun_part_01",
        "stun_part_02",
        "throw",
        "chandelier_00",
        "chandelier_04",
        "hit_part_01",
        "hit_part_02",
        "hit_part_03",
        "death",
    ],
    "PC _ Computer - Blasphemous 2 - Enemies - Charred Skull.png": [
        "idle",
        "attack",
        "death",
    ],
    "PC _ Computer - Blasphemous 2 - Enemies - Dummy Variant.png": [
        "idle",
        "transition_part_01",
        "transition_part_02",
        "hit",
        "attack_01",
        "attack_02",
        "death",
        "spawn",
    ],
    "PC _ Computer - Blasphemous 2 - Enemies - Tumble Thorn.png": [
        "platform_hugger_00",
        "platform_hugger_11",
    ],
}


def main() -> None:
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if not BACKUP.exists():
        shutil.copy2(MANIFEST, BACKUP)

    planned: list[tuple[Path, Path, dict[str, object], str]] = []
    report: list[dict[str, object]] = []
    for item in data["files"]:
        actions = item.get("actions", [])
        if not actions:
            continue
        names = NAMES.get(item["source"])
        if names is None:
            raise RuntimeError(f"Missing name map for {item['source']}")
        if len(names) != len(actions):
            raise RuntimeError(f"Name count mismatch for {item['source']}: {len(names)} != {len(actions)}")
        if len(set(names)) != len(names):
            raise RuntimeError(f"Duplicate names for {item['source']}")

        for action, name in zip(actions, names):
            old_relative = Path(action["file"])
            old_path = ROOT / old_relative
            new_path = old_path.with_name(f"{name}.png")
            if not old_path.exists():
                raise FileNotFoundError(old_path)
            if new_path.exists() and new_path != old_path:
                raise FileExistsError(new_path)
            planned.append((old_path, new_path, action, name))

    for old_path, new_path, action, name in planned:
        old_relative = old_path.relative_to(ROOT).as_posix()
        old_path.rename(new_path)
        new_relative = new_path.relative_to(ROOT).as_posix()
        action["original_file"] = old_relative
        action["file"] = new_relative
        action["action_name"] = name
        report.append(
            {
                "source": next(item["source"] for item in data["files"] if action in item.get("actions", [])),
                "index": action["index"],
                "old": old_relative,
                "new": new_relative,
                "action_name": name,
            }
        )

    MANIFEST.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Renamed {len(report)} action strip files")
    print(REPORT)


if __name__ == "__main__":
    main()
