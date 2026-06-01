using System.Collections.Generic;

namespace ElectronicHandyman.Avalonia.Services;

public class DetectionResult
{
    public List<BoundingBox> BoundingBoxes { get; init; } = [];
    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
}

public readonly record struct BoundingBox(int X, int Y, int Width, int Height, float Confidence = 0f);
