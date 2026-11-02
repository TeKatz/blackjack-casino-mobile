#!/usr/bin/env python3
"""Extract the existing verified atlas artwork into compact Android JPEG assets."""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/BlackjackCasino.Desktop/Assets/CollectionArt"
DEST = ROOT / "src/BlackjackCasino.Mobile/Resources/Images/Collection"
REPORT = ROOT / "artifacts/mobile-album-art-report.json"


def prepare() -> None:
    manifest_bytes = (SOURCE / "manifest.json").read_bytes()
    manifest = json.loads(manifest_bytes)
    cards = manifest["Cards"]
    if len(cards) != 136 or len({card["Id"] for card in cards}) != 136:
        raise ValueError("Expected all 136 distinct catalog cards")
    atlases: dict[str, Image.Image] = {}
    for atlas in manifest["Atlases"]:
        path = SOURCE / atlas["File"]
        if path.parent != SOURCE or hashlib.sha256(path.read_bytes()).hexdigest() != atlas["Sha256"]:
            raise ValueError(f"Unverified atlas: {atlas['Id']}")
        with Image.open(path) as original:
            if original.size != (atlas["Width"], atlas["Height"]):
                raise ValueError(f"Wrong atlas size: {atlas['Id']}")
            atlases[atlas["Id"]] = original.convert("RGB")
    DEST.mkdir(parents=True, exist_ok=True)
    expected: set[str] = set()
    source_hashes: set[str] = set()
    output_hashes: set[str] = set()
    output = []
    for card in cards:
        image = atlases[card["Atlas"]]
        x, y, width, height = (card[key] for key in ("X", "Y", "Width", "Height"))
        if x < 0 or y < 0 or width < 20 or height < 20 or x + width > image.width or y + height > image.height:
            raise ValueError(f"Invalid card crop: {card['Id']}")
        crop = image.crop((x, y, x + width, y + height))
        pixel_hash = hashlib.sha256(crop.tobytes()).hexdigest()
        if pixel_hash in source_hashes:
            raise ValueError(f"Repeated card illustration: {card['Id']}")
        source_hashes.add(pixel_hash)
        # Keep regular cards at native resolution; cap only large ultra-rare crops.
        if crop.width > 416:
            crop = crop.resize((416, round(crop.height * 416 / crop.width)), Image.Resampling.LANCZOS)
        filename = "album_" + card["Id"].replace("-", "_") + ".jpg"
        if not re.fullmatch(r"[a-z][a-z0-9_]*\.jpg", filename):
            raise ValueError(f"Not an Android resource name: {filename}")
        expected.add(filename)
        path = DEST / filename
        crop.save(path, "JPEG", quality=92, subsampling=0, optimize=True)
        with Image.open(path) as decoded:
            decoded.load()
            if decoded.size != crop.size:
                raise ValueError(f"Bad generated image: {filename}")
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest in output_hashes:
            raise ValueError(f"Repeated generated image: {filename}")
        output_hashes.add(digest)
        output.append({"Id": card["Id"], "File": filename, "Width": crop.width,
                       "Height": crop.height, "Sha256": digest, "Bytes": path.stat().st_size})
    for stale in DEST.glob("*.jpg"):
        if stale.name not in expected:
            stale.unlink()
    report = {"Version": 1, "Cards": len(output), "Atlases": len(atlases),
              "SourceManifestSha256": hashlib.sha256(manifest_bytes).hexdigest(),
              "TotalBytes": sum(item["Bytes"] for item in output), "Images": output}
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    (DEST / ".complete").write_text(report["SourceManifestSha256"], encoding="ascii")
    print(f"PASS: {len(output)} unique Android card images from {len(atlases)} verified atlases; "
          f"{report['TotalBytes'] / 1_000_000:.2f} MB total")
    for image in atlases.values():
        image.close()


if __name__ == "__main__":
    prepare()
