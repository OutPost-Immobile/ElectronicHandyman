namespace ElectronicHandyman.App.Services;

public class DetectionResult
{
    /// <summary>
    /// Bounding rectangles in original frame coordinate space.
    /// </summary>
    public List<BoundingBox> BoundingBoxes { get; init; } = [];

    /// <summary>
    /// Cropped image byte arrays (PNG encoded) for each detected component.
    /// </summary>
    public List<byte[]> CroppedImages { get; init; } = [];

    /// <summary>
    /// Width of the original decoded frame in pixels.
    /// </summary>
    public int FrameWidth { get; init; }

    /// <summary>
    /// Height of the original decoded frame in pixels.
    /// </summary>
    public int FrameHeight { get; init; }
}

public readonly record struct BoundingBox(int X, int Y, int Width, int Height);
