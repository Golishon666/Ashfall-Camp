from pathlib import Path

from PIL import Image


base = Path("Assets/AshfallCamp/Art/UI/Production/Characters")
bad = []
total = 0

for path in sorted(base.glob("*.png")):
    image = Image.open(path).convert("RGBA")
    width, height = image.size
    total += 1
    pixels = image.load()
    corners = [
        pixels[0, 0][3],
        pixels[width - 1, 0][3],
        pixels[0, height - 1][3],
        pixels[width - 1, height - 1][3],
    ]
    alpha = image.getchannel("A").getextrema()
    if alpha[1] == 0 or any(value != 0 for value in corners):
        bad.append((path.name, width, height, alpha, corners))

print(f"CheckedPNG={total}")
print(f"BadAlphaOrCorners={len(bad)}")
for item in bad:
    print(item)
