using Tesseract;
using OpenCvSharp;

namespace Services;

public class ImageProcessing
{
    public static string ProcessImage(byte[] imageBytes)
    {
        Console.WriteLine("|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");

        // 1. Decode to grayscale
        using var src = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);

        // 2. Apply CLAHE for contrast normalization (before resizing)
        using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
        using var claheResult = new Mat();
        clahe.Apply(src, claheResult);

        // 3. Resize 3x with cubic interpolation
        using var resized = new Mat();
        Cv2.Resize(claheResult, resized, new Size(0, 0), 3.0, 3.0, InterpolationFlags.Cubic);

        // 4. Bilateral filter — reduces noise while preserving edges
        using var filtered = new Mat();
        Cv2.BilateralFilter(resized, filtered, 9, 75, 75);

        // 5. Otsu's threshold — dark text on white background
        using var thresh = new Mat();
        Cv2.Threshold(filtered, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

        // 6. Light morphological close to fill small gaps in letter strokes
        using var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
        using var closed = new Mat();
        Cv2.MorphologyEx(thresh, closed, MorphTypes.Close, closeKernel);

        // 7. Add generous white border — Tesseract needs margin around text
        using var padded = new Mat();
        Cv2.CopyMakeBorder(closed, padded, 40, 40, 40, 40, BorderTypes.Constant, new Scalar(255));

        // 8. Save debug image
        Cv2.ImWrite("processed_image.jpg", padded);

        // 9. Tesseract OCR — run with multiple PageSegModes and pick best
        var tesseractPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        using var engine = new TesseractEngine(tesseractPath, "eng", EngineMode.Default);
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
        engine.SetVariable("user_defined_dpi", "300");

        var text = ReadTextBestEffort(padded, engine);

        Console.WriteLine($"Raw OCR: [{text}]");

        // 10. Extract chip name from first two lines
        var chipName = ExtractChipName(text);
        Console.WriteLine($"Chip name: [{chipName}]");

        // 11. Normalize
        var normalizedText = OcrTextNormalizer.Normalize(chipName);
        Console.WriteLine($"Normalized: [{normalizedText}]");

        return normalizedText;
    }

    /// <summary>
    /// Tries multiple PageSegModes and returns the result with highest confidence.
    /// </summary>
    private static string ReadTextBestEffort(Mat image, TesseractEngine engine)
    {
        Cv2.ImEncode(".png", image, out byte[] imageBytes);

        var modes = new[] { PageSegMode.Auto, PageSegMode.SingleBlock, PageSegMode.SingleColumn };
        string bestText = "";
        float bestConf = -1;

        foreach (var mode in modes)
        {
            using var img = Pix.LoadFromMemory(imageBytes);
            engine.DefaultPageSegMode = mode;
            using var page = engine.Process(img);
            var text = page.GetText().Trim();
            var conf = page.GetMeanConfidence();

            Console.WriteLine($"  Mode {mode}: [{text}] conf={conf:F2}");

            if (conf > bestConf && !string.IsNullOrWhiteSpace(text))
            {
                bestConf = conf;
                bestText = text;
            }
        }

        return bestText;
    }

    /// <summary>
    /// Extracts the chip name from OCR text by taking the first two non-empty lines.
    /// </summary>
    private static string ExtractChipName(string rawOcrText)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText))
            return string.Empty;

        var lines = rawOcrText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(2)
            .ToArray();

        return string.Join("", lines);
    }
}
