import re
from pathlib import Path

from PIL import Image


CHAR_DIR = Path("Assets/AshfallCamp/Art/UI/Production/Characters")


def replace_default_max_size(text: str) -> str:
    marker = "    buildTarget: DefaultTexturePlatform"
    start = text.find(marker)
    if start < 0:
        return text
    next_platform = text.find("  - serializedVersion:", start + len(marker))
    end = next_platform if next_platform >= 0 else len(text)
    block = text[start:end]
    block = re.sub(r"(\n    maxTextureSize: )\d+", r"\g<1>2048", block, count=1)
    return text[:start] + block + text[end:]


def normalize_meta(png_path: Path) -> None:
    meta_path = Path(str(png_path) + ".meta")
    if not meta_path.exists():
        return

    with Image.open(png_path) as image:
        width, height = image.size

    stem = png_path.stem
    sprite_name = f"{stem}_0"
    text = meta_path.read_text(encoding="utf-8-sig")

    text = re.sub(r"(\n    second: ).+", rf"\g<1>{sprite_name}", text, count=1)
    text = re.sub(r"(\n      name: ).+", rf"\g<1>{sprite_name}", text, count=1)
    text = re.sub(r"(\n  spriteMode: )\d+", r"\g<1>1", text, count=1)
    text = replace_default_max_size(text)

    rect = (
        "      rect:\n"
        "        serializedVersion: 2\n"
        "        x: 0\n"
        "        y: 0\n"
        f"        width: {width}\n"
        f"        height: {height}"
    )
    text = re.sub(
        r"      rect:\n\s+serializedVersion: 2\n\s+x: 0\n\s+y: 0\n\s+width: \d+\n\s+height: \d+",
        rect,
        text,
        count=1,
    )

    text = re.sub(
        r"(\n    nameFileIdTable:\n)\s+[^:\n]+:",
        rf"\g<1>      {sprite_name}:",
        text,
        count=1,
    )

    meta_path.write_text(text, encoding="utf-8", newline="\n")
    print(f"Normalized {meta_path}")


def main() -> None:
    for png_path in sorted(CHAR_DIR.glob("*.png")):
        normalize_meta(png_path)


if __name__ == "__main__":
    main()
