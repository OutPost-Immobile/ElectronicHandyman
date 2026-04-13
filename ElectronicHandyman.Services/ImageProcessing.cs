using Tesseract;

namespace Services;
using OpenCvSharp;
    public class ImageProcessing
    {
        public static void ProcessImage(byte[] imageBytes)
        {
            Console.WriteLine("|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
            using (var src = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale))
            {
                    using Mat resized = new Mat();
                    Cv2.Resize(src, resized, new OpenCvSharp.Size(0,0), 2.0, 2.0, InterpolationFlags.Cubic);
                    using Mat blurred = new Mat();
                    Cv2.GaussianBlur(resized, blurred, new OpenCvSharp.Size(5,5), 0);
                    using Mat inverted = new Mat();
                    Cv2.BitwiseNot(blurred, inverted);
                    
                    using Mat thresh = new Mat();
                    Cv2.AdaptiveThreshold(inverted, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 131, 8);

                    var tesseract = Path.Combine(AppContext.BaseDirectory, "tessdata");
                    var engine = new TesseractEngine(tesseract, "eng", EngineMode.Default);
                    engine.DefaultPageSegMode = PageSegMode.AutoOsd; 
                    engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789- ");
                    
                    Cv2.ImWrite("processed_image.jpg", thresh);
                    
                    Console.WriteLine(ReadTextFromImage(thresh, engine));
            }
        }
        
        private static string ReadTextFromImage(Mat image, TesseractEngine engine)
        {
            Cv2.ImEncode(".jpg", image, out byte[] imageBytes);
            
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;

            using (var img = Pix.LoadFromMemory(imageBytes))
            {
                using (var page = engine.Process(img)) 
                { 
                    return page.GetText().Trim(); 
                }
            }
        }
    }