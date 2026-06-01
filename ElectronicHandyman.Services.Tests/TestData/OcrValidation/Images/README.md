# OCR Validation Test Images

Place test images in this directory for OCR validation testing.

## Requirements

- Images should be JPEG (.jpg) or PNG (.png) format
- Each image filename must have a corresponding entry in `../ground_truth.csv`
- Images should contain chip markings/text that the OCR pipeline will attempt to read

## Ground Truth CSV Format

The `ground_truth.csv` file in the parent directory maps each image to its expected OCR output:

```csv
FileName,ExpectedChipName,Category
chip001.jpg,STM32F103C8T6,normal
chip002.png,LM358N,low_light
```

## Categories

- `normal` — Standard lighting and orientation
- `low_light` — Poor lighting conditions
- `rotated` — Chip text is rotated
- `blurry` — Image is out of focus
- `occluded` — Chip text is partially obscured
- `small_chip` — Very small chip markings

## Notes

- Tests will skip gracefully if no images are present in this directory
- Images referenced in ground_truth.csv but missing from this directory will be reported as skipped files
