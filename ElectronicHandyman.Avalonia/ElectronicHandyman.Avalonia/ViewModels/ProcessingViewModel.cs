using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicHandyman.Avalonia.Services;
using OpenCvSharp;

namespace ElectronicHandyman.Avalonia.ViewModels;

public partial class ProcessingViewModel : ViewModelBase
{
    private readonly byte[] _imageBytes;
    private readonly VideoFrameProcessor _processor;
    private readonly ChipIdentificationService _identificationService;
    private readonly Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>> _navigateToBatchResult;
    private readonly Action<string, string> _navigateToResult;
    private readonly Action _navigateBack;

    [ObservableProperty]
    private string _statusText = "Initializing...";

    [ObservableProperty]
    private Bitmap? _previewImage;

    // For design time
    public ProcessingViewModel()
    {
        _imageBytes = Array.Empty<byte>();
        _processor = new VideoFrameProcessor();
        _identificationService = new ChipIdentificationService("");
        _navigateToBatchResult = (_, _, _) => { };
        _navigateToResult = (_, _) => { };
        _navigateBack = () => { };
    }

    public ProcessingViewModel(
        byte[] imageBytes,
        VideoFrameProcessor processor,
        ChipIdentificationService identificationService,
        Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>> navigateToBatchResult,
        Action<string, string> navigateToResult,
        Action navigateBack)
    {
        _imageBytes = imageBytes;
        _processor = processor;
        _identificationService = identificationService;
        _navigateToBatchResult = navigateToBatchResult;
        _navigateToResult = navigateToResult;
        _navigateBack = navigateBack;

        try
        {
            using var stream = new MemoryStream(_imageBytes);
            PreviewImage = new Bitmap(stream);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigateBack();
    }

    public async Task ProcessImageAsync()
    {
        try
        {
            Dispatcher.UIThread.Post(() => StatusText = "Detecting chips...");

            // Run YOLO detection
            var detection = await Task.Run(() => _processor.ProcessFrame(_imageBytes));

            if (detection.BoundingBoxes.Count == 0)
            {
                Dispatcher.UIThread.Post(() => StatusText = "No chips detected. Sending full image...");
                var singleResult = await _identificationService.IdentifyChipAsync(_imageBytes);
                if (singleResult.IsSuccess && singleResult.SvgContent != null)
                {
                    Dispatcher.UIThread.Post(() => _navigateToResult(singleResult.ChipName ?? "Unknown", singleResult.SvgContent));
                }
                else
                {
                    Dispatcher.UIThread.Post(() => StatusText = $"Not identified: {singleResult.ErrorMessage}");
                    await Task.Delay(2000);
                    Dispatcher.UIThread.Post(() => _navigateBack());
                }
                return;
            }

            Dispatcher.UIThread.Post(() => StatusText = $"Found {detection.BoundingBoxes.Count} chip(s), identifying...");

            var croppedImages = new List<byte[]>();
            using var fullImage = Cv2.ImDecode(_imageBytes, ImreadModes.Color);

            foreach (var box in detection.BoundingBoxes)
            {
                int x = Math.Max(0, box.X);
                int y = Math.Max(0, box.Y);
                int w = Math.Min(box.Width, fullImage.Width - x);
                int h = Math.Min(box.Height, fullImage.Height - y);
                if (w <= 0 || h <= 0) continue;

                using var cropped = new Mat(fullImage, new Rect(x, y, w, h));
                var jpegBytes = cropped.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
                croppedImages.Add(jpegBytes);
            }

            var results = await _identificationService.IdentifyBatchAsync(croppedImages);
            
            var identifiedCount = results.Count(r => r.IsSuccess);
            Dispatcher.UIThread.Post(() => StatusText = $"{identifiedCount}/{results.Count} identified. Loading results...");
            
            await Task.Delay(500); // Brief pause to read status

            Dispatcher.UIThread.Post(() => _navigateToBatchResult(_imageBytes, detection.BoundingBoxes, results));
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => StatusText = $"Error: {ex.Message}");
            await Task.Delay(3000);
            Dispatcher.UIThread.Post(() => _navigateBack());
        }
    }
}
