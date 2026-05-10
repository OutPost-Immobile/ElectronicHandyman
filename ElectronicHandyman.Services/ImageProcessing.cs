using Tesseract;
using OpenCvSharp;

namespace Services;

public class ImageProcessing
{
    public static string ProcessImage(byte[] imageBytes, string? saveProcessedPath = null)
    {
        Console.WriteLine("|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");

        // 1. Decode to grayscale
        using var src = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);

        // 2. Apply CLAHE for contrast normalization
        using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
        using var claheResult = new Mat();
        clahe.Apply(src, claheResult);

        // 3. Resize 4x with cubic interpolation (larger = better for small text)
        using var resized = new Mat();
        Cv2.Resize(claheResult, resized, new Size(0, 0), 4.0, 4.0, InterpolationFlags.Cubic);

        // 4. Bilateral filter — reduces noise while preserving edges
        using var filtered = new Mat();
        Cv2.BilateralFilter(resized, filtered, 9, 75, 75);

        // 4b. Sharpen to make text edges crisper
        using var sharpened = new Mat();
        using var blurForSharp = new Mat();
        Cv2.GaussianBlur(filtered, blurForSharp, new Size(0, 0), 3);
        Cv2.AddWeighted(filtered, 1.5, blurForSharp, -0.5, 0, sharpened);

        // 5. Determine if text is light-on-dark or dark-on-light
        //    Chips typically have white/light text on dark background
        var mean = Cv2.Mean(sharpened);
        bool isDarkBackground = mean.Val0 < 128;

        using var thresh = new Mat();
        if (isDarkBackground)
        {
            // Light text on dark bg — threshold normally (text becomes white)
            Cv2.Threshold(sharpened, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        }
        else
        {
            // Dark text on light bg — invert
            Cv2.Threshold(sharpened, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        }

        // 6. Light morphological close to fill small gaps in letter strokes
        using var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
        using var closed = new Mat();
        Cv2.MorphologyEx(thresh, closed, MorphTypes.Close, closeKernel);

        // 7. Add generous white border
        using var padded = new Mat();
        Cv2.CopyMakeBorder(closed, padded, 40, 40, 40, 40, BorderTypes.Constant, new Scalar(255));

        // Save processed image if path provided
        if (!string.IsNullOrEmpty(saveProcessedPath))
        {
            Cv2.ImWrite(saveProcessedPath, padded);
        }

        // 8. Try all 4 rotations and pick best OCR result
        var tesseractPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        using var engine = new TesseractEngine(tesseractPath, "eng", EngineMode.Default);
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
        engine.SetVariable("user_defined_dpi", "300");

        string bestText = "";
        float bestConf = -1;

        for (int rotation = 0; rotation < 4; rotation++)
        {
            using var rotated = rotation == 0 ? padded.Clone() : RotateImage(padded, rotation);
            var (text, conf) = ReadTextWithConfidence(rotated, engine);

            Console.WriteLine($"  Rotation {rotation * 90}°: [{text}] conf={conf:F2}");

            if (conf > bestConf && !string.IsNullOrWhiteSpace(text) && text.Length >= 3)
            {
                bestConf = conf;
                bestText = text;
            }
        }

        Console.WriteLine($"Raw OCR (best): [{bestText}] conf={bestConf:F2}");

        // 9. Extract chip name from first two lines
        var chipName = ExtractChipName(bestText);
        Console.WriteLine($"Chip name: [{chipName}]");

        // 10. Normalize
        var normalizedText = OcrTextNormalizer.Normalize(chipName);
        Console.WriteLine($"Normalized: [{normalizedText}]");

        return normalizedText;
    }

    private static Mat RotateImage(Mat src, int rotationCount)
    {
        var rotated = src.Clone();
        for (int i = 0; i < rotationCount; i++)
        {
            var temp = new Mat();
            Cv2.Rotate(rotated, temp, RotateFlags.Rotate90Clockwise);
            rotated.Dispose();
            rotated = temp;
        }
        return rotated;
    }

    private static (string text, float confidence) ReadTextWithConfidence(Mat image, TesseractEngine engine)
    {
        Cv2.ImEncode(".png", image, out byte[] imageBytes);

        string bestText = "";
        float bestConf = -1;

        var modes = new[] { PageSegMode.Auto, PageSegMode.SingleBlock };

        foreach (var mode in modes)
        {
            using var img = Pix.LoadFromMemory(imageBytes);
            engine.DefaultPageSegMode = mode;
            using var page = engine.Process(img);
            var text = page.GetText().Trim();
            var conf = page.GetMeanConfidence();

            if (conf > bestConf && !string.IsNullOrWhiteSpace(text))
            {
                bestConf = conf;
                bestText = text;
            }
        }

        return (bestText, bestConf);
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
