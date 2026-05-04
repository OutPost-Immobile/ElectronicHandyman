using OpenCvSharp;

namespace ElectronicHandyman.App.Services;

public class VideoFrameProcessor
{
    private const int AnalysisWidth = 640; 
    
    public List<byte[]> ProcessCameraFrameAndCrop(byte[] cameraFrameBytes)
    {
        var croppedElements = new List<byte[]>();
        
        using var originalFrame = Cv2.ImDecode(cameraFrameBytes, ImreadModes.Color);
        if (originalFrame.Empty())
        {
            return croppedElements;
        }
        
        float scale = (float)AnalysisWidth / originalFrame.Width;
        int analysisHeight = (int)(originalFrame.Height * scale);

        using var smallFrame = new Mat();
        Cv2.Resize(originalFrame, smallFrame, new OpenCvSharp.Size(AnalysisWidth, analysisHeight));
        
        using var gray = new Mat();
        Cv2.CvtColor(smallFrame, gray, ColorConversionCodes.BGR2GRAY);
        
        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

        using var morphImage = new Mat();
        using var structElement = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(7, 7));
        Cv2.MorphologyEx(blurred, morphImage, MorphTypes.Close, structElement);

        using var binaryImage = new Mat();
        Cv2.AdaptiveThreshold(morphImage, binaryImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, 11, 2);

        Cv2.FindContours(binaryImage, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < 200) continue; 

            var rect = Cv2.BoundingRect(contour);
            var aspectRatio = (float)rect.Width / rect.Height;
            var boxArea = rect.Width * rect.Height;
            var extent = area / boxArea;

           
            if (aspectRatio >= 0.80f && aspectRatio <= 1.25f && extent >= 0.70f)
            {
                int originalX = (int)(rect.X / scale);
                int originalY = (int)(rect.Y / scale);
                int originalWidth = (int)(rect.Width / scale);
                int originalHeight = (int)(rect.Height / scale);

                var originalRect = new OpenCvSharp.Rect(originalX, originalY, originalWidth, originalHeight);

                // Zabezpieczenie, aby wycinek nie wyszedł poza granice obrazu
                if (originalRect.X >= 0 && originalRect.Y >= 0 && 
                    originalRect.Right <= originalFrame.Width && 
                    originalRect.Bottom <= originalFrame.Height)
                {
                    // Wycinka z dużej, ostrej klatki
                    using var croppedMat = new Mat(originalFrame, originalRect);
                    
                    // Zapis wycinka do tablicy bajtów (np. jako PNG)
                    Cv2.ImEncode(".png", croppedMat, out var croppedBytes);
                    croppedElements.Add(croppedBytes);
                }
            }
        }

        // Zwracamy listę wyciętych obrazków. Aplikacja mobilna decyduje co z nimi zrobić.
        return croppedElements;
    }
}