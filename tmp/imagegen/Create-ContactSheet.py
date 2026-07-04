import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def make_sheet(paths, out_path: Path, columns: int, thumb_h: int) -> None:
    font = ImageFont.load_default()
    label_h = 24
    pad = 12
    thumb_w = int(thumb_h * 0.82)
    rows = (len(paths) + columns - 1) // columns
    sheet_w = columns * thumb_w + (columns + 1) * pad
    sheet_h = rows * (thumb_h + label_h) + (rows + 1) * pad

    sheet = Image.new("RGB", (sheet_w, sheet_h), (28, 28, 28))
    draw = ImageDraw.Draw(sheet)

    for i, path in enumerate(paths):
        image = Image.open(path).convert("RGBA")
        bbox = image.getbbox()
        if bbox:
            image = image.crop(bbox)
        image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)

        cell_x = pad + (i % columns) * (thumb_w + pad)
        cell_y = pad + (i // columns) * (thumb_h + label_h + pad)
        x = cell_x + (thumb_w - image.width) // 2
        y = cell_y + (thumb_h - image.height) // 2

        tile = Image.new("RGB", (image.width, image.height), (8, 8, 8))
        tile.paste(image, mask=image.getchannel("A"))
        sheet.paste(tile, (x, y))

        label = path.stem.replace("ui_character_", "")
        draw.text((cell_x, cell_y + thumb_h + 4), label[-24:], fill=(220, 220, 220), font=font)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)
    print(out_path)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--glob", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--columns", type=int, default=6)
    parser.add_argument("--thumb-height", type=int, default=220)
    args = parser.parse_args()

    paths = sorted(Path().glob(args.glob))
    if not paths:
        raise SystemExit(f"No files matched {args.glob}")
    make_sheet(paths, Path(args.out), args.columns, args.thumb_height)


if __name__ == "__main__":
    main()
