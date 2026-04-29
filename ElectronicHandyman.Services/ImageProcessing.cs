using Tesseract;
using OpenCvSharp;

namespace Services;

public class ImageProcessing
{
    public static string ProcessImage(byte[] imageBytes)
    {
        Console.WriteLine("|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
        using var src = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);

        using var resized = new Mat();
        Cv2.Resize(src, resized, new Size(0,0), 2.0, 2.0, InterpolationFlags.Cubic);
        
        using var blurred = new Mat();
        Cv2.GaussianBlur(resized, blurred, new Size(5,5), 0);
        
        using var inverted = new Mat();
        Cv2.BitwiseNot(blurred, inverted);
                
        using var thresh = new Mat();
        Cv2.AdaptiveThreshold(inverted, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 131, 8);

        var tesseract = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var engine = new TesseractEngine(tesseract, "eng", EngineMode.Default);
        engine.DefaultPageSegMode = PageSegMode.AutoOsd; 
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789- ");
                
        Cv2.ImWrite("processed_image.jpg", thresh);
        
        var text = ReadTextFromImage(thresh, engine);
        
        Console.WriteLine(text);
        
        return text;
    }
    
    private static string ReadTextFromImage(Mat image, TesseractEngine engine)
    {
        Cv2.ImEncode(".jpg", image, out byte[] imageBytes);
        
        engine.DefaultPageSegMode = PageSegMode.SingleBlock;

        using var img = Pix.LoadFromMemory(imageBytes);

        using var page = engine.Process(img);

        return page.GetText().Trim();
    }
}