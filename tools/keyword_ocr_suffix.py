from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from keyword_ocr_fast import ROOT, MANIFEST, WORDS, best_score, source_mask, templates


def common_prefix_width(masks: list[np.ndarray]) -> int:
    valid = [mask for mask in masks if mask.sum() >= 20]
    if len(valid) < 2:
        return 0
    width = min(mask.shape[1] for mask in valid)
    reference = valid[0]
    mismatch_streak = 0
    last_shared = 0
    for x in range(width):
        same = sum(np.array_equal(mask[:, x], reference[:, x]) for mask in valid) / len(valid)
        if same >= 0.8:
            last_shared = x + 1
            mismatch_streak = 0
        else:
            mismatch_streak += 1
            if mismatch_streak >= 3:
                return max(0, last_shared - 2)
    return last_shared


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
        with Image.open(ROOT / item["source"]) as source:
            masks = [source_mask(source, 0, action["source_rect"]["y"]) for action in item["actions"]]
        prefix = common_prefix_width(masks)
        print(f"SOURCE: {item['source']} | common_prefix_px={prefix}")
        action_scores: list[dict[str, object]] = []
        for action, mask in zip(item["actions"], masks):
            suffix = mask[:, prefix:]
            scores = sorted(((best_score(suffix, variants), word) for word, variants in model.items()), reverse=True)[:10]
            print(f"{action['index']:03d}: " + ", ".join(f"{word}={score:.3f}" for score, word in scores))
            action_scores.append({"index": action["index"], "scores": [{"word": word, "score": round(score, 4)} for score, word in scores]})
        report.append({"source": item["source"], "common_prefix_px": prefix, "actions": action_scores})
    output = Path("sprite_analysis") / f"keyword_suffix_{args.filter.replace(' ', '_')}.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
