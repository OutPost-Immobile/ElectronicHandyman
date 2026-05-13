using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ElectronicHandyman.Avalonia.Services;

/// <summary>
/// Runs YOLOv8 object detection using ONNX Runtime.
/// Expects a model exported with imgsz=640.
/// </summary>
public sealed class YoloDetector : IDisposable
{
    private const int ModelInputSize = 640;
    private const float ConfidenceThreshold = 0.4f;
    private const float NmsIouThreshold = 0.45f;

    private readonly InferenceSession _session;

    public YoloDetector(byte[] modelBytes)
    {
        _session = new InferenceSession(modelBytes);
    }

    public YoloDetector(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }

    public DetectionResult Detect(byte[] imageBytes)
    {
        using var image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        if (image.Empty())
        {
            return new DetectionResult();
        }

        return Detect(image);
    }

    public DetectionResult Detect(Mat image)
    {
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        // Preprocess: letterbox resize to 640x640
        var (inputTensor, ratioW, ratioH, padX, padY) = Preprocess(image);

        // Run inference
        var inputName = _session.InputNames[0];
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using var results = _session.Run(inputs);
        var outputTensor = results.First().AsTensor<float>();

        // Postprocess: parse detections
        var boxes = Postprocess(outputTensor, ratioW, ratioH, padX, padY, originalWidth, originalHeight);

        return new DetectionResult
        {
            BoundingBoxes = boxes,
            FrameWidth = originalWidth,
            FrameHeight = originalHeight
        };
    }

    private (DenseTensor<float> tensor, float ratioW, float ratioH, float padX, float padY) Preprocess(Mat image)
    {
        int w = image.Width;
        int h = image.Height;

        // Calculate scale to fit in 640x640 maintaining aspect ratio
        float scale = Math.Min((float)ModelInputSize / w, (float)ModelInputSize / h);
        int newW = (int)(w * scale);
        int newH = (int)(h * scale);

        using var resized = new Mat();
        Cv2.Resize(image, resized, new OpenCvSharp.Size(newW, newH));

        // Create padded image (letterbox)
        float padX = (ModelInputSize - newW) / 2f;
        float padY = (ModelInputSize - newH) / 2f;

        using var padded = new Mat(ModelInputSize, ModelInputSize, MatType.CV_8UC3, new Scalar(114, 114, 114));
        resized.CopyTo(padded[new Rect((int)padX, (int)padY, newW, newH)]);

        // Convert to float tensor [1, 3, 640, 640], normalized to 0-1
        var tensor = new DenseTensor<float>(new[] { 1, 3, ModelInputSize, ModelInputSize });

        var dataSize = ModelInputSize * ModelInputSize * 3;
        var pixelData = new byte[dataSize];
        System.Runtime.InteropServices.Marshal.Copy(padded.Data, pixelData, 0, dataSize);

        for (int y = 0; y < ModelInputSize; y++)
        {
            for (int x = 0; x < ModelInputSize; x++)
            {
                int idx = (y * ModelInputSize + x) * 3;
                tensor[0, 0, y, x] = pixelData[idx + 2] / 255f; // R
                tensor[0, 1, y, x] = pixelData[idx + 1] / 255f; // G
                tensor[0, 2, y, x] = pixelData[idx + 0] / 255f; // B
            }
        }

        float ratioW = 1f / scale;
        float ratioH = 1f / scale;

        return (tensor, ratioW, ratioH, padX, padY);
    }

    private List<BoundingBox> Postprocess(Tensor<float> output, float ratioW, float ratioH, float padX, float padY, int imgW, int imgH)
    {
        // YOLOv8 output shape: [1, numClasses+4, numDetections]
        // Transpose to [numDetections, numClasses+4]
        var shape = output.Dimensions;
        int numFeatures = shape[1]; // 4 + numClasses
        int numDetections = shape[2];

        var candidates = new List<(float x1, float y1, float x2, float y2, float confidence)>();

        for (int i = 0; i < numDetections; i++)
        {
            // Find max class confidence
            float maxConf = 0;
            for (int c = 4; c < numFeatures; c++)
            {
                float conf = output[0, c, i];
                if (conf > maxConf) maxConf = conf;
            }

            if (maxConf < ConfidenceThreshold) continue;

            // Get box coordinates (cx, cy, w, h)
            float cx = output[0, 0, i];
            float cy = output[0, 1, i];
            float bw = output[0, 2, i];
            float bh = output[0, 3, i];

            // Convert to x1, y1, x2, y2
            float x1 = cx - bw / 2;
            float y1 = cy - bh / 2;
            float x2 = cx + bw / 2;
            float y2 = cy + bh / 2;

            // Remove padding and scale back to original image
            x1 = (x1 - padX) * ratioW;
            y1 = (y1 - padY) * ratioH;
            x2 = (x2 - padX) * ratioW;
            y2 = (y2 - padY) * ratioH;

            // Clamp to image bounds
            x1 = Math.Max(0, Math.Min(x1, imgW));
            y1 = Math.Max(0, Math.Min(y1, imgH));
            x2 = Math.Max(0, Math.Min(x2, imgW));
            y2 = Math.Max(0, Math.Min(y2, imgH));

            candidates.Add((x1, y1, x2, y2, maxConf));
        }

        // Apply NMS
        var nmsResult = ApplyNms(candidates);

        return nmsResult.Select(d => new BoundingBox(
            (int)d.x1,
            (int)d.y1,
            (int)(d.x2 - d.x1),
            (int)(d.y2 - d.y1),
            d.confidence
        )).ToList();
    }

    private static List<(float x1, float y1, float x2, float y2, float confidence)> ApplyNms(
        List<(float x1, float y1, float x2, float y2, float confidence)> boxes)
    {
        var sorted = boxes.OrderByDescending(b => b.confidence).ToList();
        var result = new List<(float x1, float y1, float x2, float y2, float confidence)>();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            result.Add(best);
            sorted.RemoveAt(0);

            sorted.RemoveAll(b => ComputeIou(best, b) > NmsIouThreshold);
        }

        return result;
    }

    private static float ComputeIou(
        (float x1, float y1, float x2, float y2, float confidence) a,
        (float x1, float y1, float x2, float y2, float confidence) b)
    {
        float interX1 = Math.Max(a.x1, b.x1);
        float interY1 = Math.Max(a.y1, b.y1);
        float interX2 = Math.Min(a.x2, b.x2);
        float interY2 = Math.Min(a.y2, b.y2);

        float interArea = Math.Max(0, interX2 - interX1) * Math.Max(0, interY2 - interY1);
        float aArea = (a.x2 - a.x1) * (a.y2 - a.y1);
        float bArea = (b.x2 - b.x1) * (b.y2 - b.y1);

        return interArea / (aArea + bArea - interArea);
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
