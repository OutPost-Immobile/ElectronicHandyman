using System.Collections.Generic;

namespace ElectronicHandyman.Avalonia.Services;

public class VideoFrameProcessor
{
    private readonly YoloDetector? _yoloDetector;

    public VideoFrameProcessor()
    {
    }

    public VideoFrameProcessor(YoloDetector yoloDetector)
    {
        _yoloDetector = yoloDetector;
    }

    public DetectionResult ProcessFrame(byte[] cameraFrameBytes)
    {
        if (_yoloDetector != null)
        {
            return _yoloDetector.Detect(cameraFrameBytes);
        }

        // Fallback: no detection if no model loaded
        return new DetectionResult
        {
            BoundingBoxes = new List<BoundingBox>(),
            FrameWidth = 0,
            FrameHeight = 0
        };
    }
}
