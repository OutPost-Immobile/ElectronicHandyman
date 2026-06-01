#!/usr/bin/env python3
import argparse
import csv
import os
import re
import shutil
import sys
from pathlib import Path

try:
    import cv2
    from paddleocr import PaddleOCR
except Exception as exc:
    print("Missing dependencies. Install with:")
    print("  pip install opencv-python paddleocr")
    print("If you have GPU, install the GPU build of paddlepaddle.")
    print(f"Error: {exc}")
    sys.exit(1)


def is_image(path: Path) -> bool:
    return path.suffix.lower() in {".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"}


def make_safe_filename(relative_path: Path) -> str:
    safe = str(relative_path).replace(os.sep, "_")
    safe = safe.replace("/", "_")
    return re.sub(r"[^A-Za-z0-9._-]", "_", safe)


def normalize_text(text: str) -> str:
    text = re.sub(r"\s+", "", text or "")
    text = text.upper()
    return re.sub(r"[^A-Z0-9-]", "", text)


def build_ocr() -> PaddleOCR:
    return PaddleOCR(
        use_angle_cls=True,
        lang="en",
        show_log=False,
    )


def ocr_image(ocr: PaddleOCR, image_path: Path) -> str:
    img = cv2.imread(str(image_path), cv2.IMREAD_GRAYSCALE)
    if img is None:
        return ""
    img = cv2.resize(img, None, fx=3.0, fy=3.0, interpolation=cv2.INTER_CUBIC)
    img = cv2.GaussianBlur(img, (5, 5), 0)

    result = ocr.ocr(img, cls=True)
    if not result:
        return ""

    tokens = []
    for line in result:
        for item in line:
            if len(item) >= 2:
                text = item[1][0]
                if text:
                    tokens.append(text)

    return normalize_text(" ".join(tokens))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, help="Input directory with crops")
    parser.add_argument(
        "--images",
        default="../../ElectronicHandyman.Services.Tests/TestData/OcrValidation/Images",
        help="Target images directory (for tests)",
    )
    parser.add_argument(
        "--csv",
        default="../../ElectronicHandyman.Services.Tests/TestData/OcrValidation/ground_truth.csv",
        help="Output CSV path",
    )
    parser.add_argument("--category", default="normal")
    parser.add_argument("--no-copy", action="store_true")

    args = parser.parse_args()

    input_dir = Path(args.input).resolve()
    images_dir = Path(args.images).resolve()
    csv_path = Path(args.csv).resolve()

    if not input_dir.exists():
        print(f"Input directory not found: {input_dir}")
        return 1

    images_dir.mkdir(parents=True, exist_ok=True)

    files = [p for p in input_dir.rglob("*") if p.is_file() and is_image(p)]
    files.sort(key=lambda p: str(p).lower())

    if not files:
        print("No images found.")
        return 1

    ocr = build_ocr()

    with csv_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["FileName", "ExpectedChipName", "Category"])

        for idx, image_path in enumerate(files, start=1):
            rel = image_path.relative_to(input_dir)
            file_name = make_safe_filename(rel) if not args.no_copy else image_path.name

            if not args.no_copy:
                target = images_dir / file_name
                shutil.copy2(image_path, target)

            text = ocr_image(ocr, image_path)
            writer.writerow([file_name, text, args.category])

            if idx % 25 == 0:
                print(f"Processed {idx}/{len(files)}")

    print(f"CSV saved to: {csv_path}")
    print(f"Images directory: {images_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
