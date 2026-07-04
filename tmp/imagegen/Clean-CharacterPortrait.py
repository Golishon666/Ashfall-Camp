import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter, ImageChops


def clean_portrait(path: Path, strength: float) -> None:
    image = Image.open(path).convert("RGBA")
    rgb = image.convert("RGB")
    alpha = image.getchannel("A")

    median = rgb.filter(ImageFilter.MedianFilter(size=3))
    smooth = Image.blend(rgb, median, strength)
    smooth = Image.blend(smooth, smooth.filter(ImageFilter.GaussianBlur(radius=0.35)), strength * 0.35)

    original = np.asarray(rgb).astype(np.float32)
    cleaned = np.asarray(smooth).astype(np.float32)
    a = np.asarray(alpha).astype(np.float32)

    # Preserve crisp opaque edges and eyes/features by applying less cleaning
    # around high-contrast structure, but still reduce speckle inside surfaces.
    edge = ImageChops.difference(rgb, rgb.filter(ImageFilter.GaussianBlur(radius=1.1))).convert("L")
    edge_arr = np.asarray(edge).astype(np.float32) / 255.0
    opaque = (a > 0).astype(np.float32)
    detail_protect = np.clip(edge_arr * 1.8, 0.0, 0.75)
    local_strength = strength * (1.0 - detail_protect) * opaque
    local_strength = local_strength[:, :, None]

    out_rgb = original * (1.0 - local_strength) + cleaned * local_strength

    # Remove green matte fringe only on semi-transparent pixels.
    edge_mask = (a > 0) & (a < 245)
    r = out_rgb[:, :, 0]
    g = out_rgb[:, :, 1]
    b = out_rgb[:, :, 2]
    green_dominant = edge_mask & (g > (r + 18)) & (g > (b + 18))
    spill_limit = np.maximum(r, b) + 8
    g[green_dominant] = np.minimum(g[green_dominant], spill_limit[green_dominant])
    out_rgb[:, :, 1] = g

    out = np.dstack([np.clip(out_rgb, 0, 255).astype(np.uint8), a.astype(np.uint8)])
    Image.fromarray(out, mode="RGBA").save(path)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+")
    parser.add_argument("--strength", type=float, default=0.22)
    args = parser.parse_args()

    for item in args.paths:
        clean_portrait(Path(item), args.strength)
        print(f"Cleaned {item}")


if __name__ == "__main__":
    main()
