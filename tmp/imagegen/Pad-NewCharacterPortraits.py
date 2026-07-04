from pathlib import Path

from PIL import Image


base = Path("Assets/AshfallCamp/Art/UI/Production/Characters")
skip = {
    "ui_character_battle_survivor_01.png",
    "ui_character_enemy_raider_01.png",
}

for path in sorted(base.glob("ui_character_*.png")):
    if path.name in skip:
        continue

    image = Image.open(path).convert("RGBA")
    width, height = image.size
    pad_x = 48
    pad_top = 32
    pad_bottom = 96
    canvas = Image.new("RGBA", (width + pad_x * 2, height + pad_top + pad_bottom), (0, 0, 0, 0))
    canvas.paste(image, (pad_x, pad_top), image)
    canvas.save(path)
    print(f"Padded {path.name}: {width}x{height} -> {canvas.width}x{canvas.height}")
