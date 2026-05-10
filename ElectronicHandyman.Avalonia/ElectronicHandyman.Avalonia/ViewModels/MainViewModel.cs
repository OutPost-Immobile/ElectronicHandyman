using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicHandyman.Avalonia.Services;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElectronicHandyman.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICameraFrameSource _cameraSource;
    private readonly VideoFrameProcessor _processor;
    private readonly ChipIdentificationService _identificationService;
    private readonly Stopwatch _throttle = Stopwatch.StartNew();
    private readonly Stopwatch _detectionThrottle = Stopwatch.StartNew();
    private int _isProcessing;
    private Action<string, string>? _navigateToResult;

    private Bitmap? _cameraImage;
    private IReadOnlyList<BoundingBox> _boundingBoxes = Array.Empty<BoundingBox>();
    private int _frameWidth;
    private int _frameHeight;
    private int _rotationDegrees;
    private string _statusText = "Waiting for camera...";
    private bool _isIdentifying;
    private byte[]? _lastFrameBytes;
    private List<ChipIdentificationResult> _identificationResults = new();

    public MainViewModel() : this(new NoopCameraFrameSource(), new VideoFrameProcessor(), new ChipIdentificationService("http://10.0.2.2:5000"))
    {
    }

    public MainViewModel(ICameraFrameSource cameraSource, VideoFrameProcessor processor, ChipIdentificationService identificationService)
    {
        _cameraSource = cameraSource;
        _processor = processor;
        _identificationService = identificationService;
        _cameraSource.FrameReady += OnFrameReady;

        if (cameraSource is NoopCameraFrameSource)
        {
            _statusText = "Camera not available on this platform.";
        }
    }

    public Bitmap? CameraImage
    {
        get => _cameraImage;
        private set
        {
            var old = _cameraImage;
            if (SetProperty(ref _cameraImage, value))
            {
                old?.Dispose();
            }
        }
    }

    public IReadOnlyList<BoundingBox> BoundingBoxes
    {
        get => _boundingBoxes;
        private set => SetProperty(ref _boundingBoxes, value);
    }

    public int FrameWidth
    {
        get => _frameWidth;
        private set => SetProperty(ref _frameWidth, value);
    }

    public int FrameHeight
    {
        get => _frameHeight;
        private set => SetProperty(ref _frameHeight, value);
    }

    public int RotationDegrees
    {
        get => _rotationDegrees;
        private set => SetProperty(ref _rotationDegrees, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsIdentifying
    {
        get => _isIdentifying;
        private set => SetProperty(ref _isIdentifying, value);
    }

    public List<ChipIdentificationResult> IdentificationResults
    {
        get => _identificationResults;
        private set => SetProperty(ref _identificationResults, value);
    }

    public async Task StartAsync()
    {
        try
        {
            StatusText = "Starting camera...";
            await _cameraSource.StartAsync();
            if (_cameraSource.IsRunning)
            {
                StatusText = "Camera running, waiting for frames...";
            }
            else
            {
                StatusText = "Waiting for camera permission...";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Camera error: {ex.Message}";
        }
    }

    public Task StopAsync() => _cameraSource.StopAsync();

    public void SetNavigateToResult(Action<string, string> navigate)
    {
        _navigateToResult = navigate;
    }

    [RelayCommand(CanExecute = nameof(CanIdentify))]
    private async Task IdentifyChipsAsync()
    {
        var frameBytes = _lastFrameBytes;

        if (frameBytes == null)
        {
            StatusText = "No frame available.";
            return;
        }

        IsIdentifying = true;
        StatusText = "Capturing frame...";

        try
        {
            // Run YOLO detection on captured frame
            StatusText = "Detecting chips...";
            var detection = await Task.Run(() => _processor.ProcessFrame(frameBytes));

            if (detection.BoundingBoxes.Count == 0)
            {
                StatusText = "No chips detected. Sending full image...";
                var singleResult = await _identificationService.IdentifyChipAsync(frameBytes);
                if (singleResult.IsSuccess && _navigateToResult != null && singleResult.SvgContent != null)
                {
                    _navigateToResult(singleResult.ChipName ?? "Unknown", singleResult.SvgContent);
                }
                else
                {
                    StatusText = $"Not identified: {singleResult.ErrorMessage}";
                }
                return;
            }

            StatusText = $"Found {detection.BoundingBoxes.Count} chip(s), identifying...";

            // Crop each bounding box and send batch
            var croppedImages = new List<byte[]>();
            using var fullImage = Cv2.ImDecode(frameBytes, ImreadModes.Color);

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
            IdentificationResults = results;

            var identified = results.Where(r => r.IsSuccess).ToList();
            StatusText = $"{identified.Count}/{results.Count} identified";

            // Navigate to result screen
            if (_navigateToBatchResult != null)
            {
                _navigateToBatchResult(frameBytes, detection.BoundingBoxes, results);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsIdentifying = false;
        }
    }

    private bool CanIdentify() => !_isIdentifying && _lastFrameBytes != null;

    private Func<Task<byte[]?>>? _pickPhotoFunc;

    public void SetPickPhotoFunc(Func<Task<byte[]?>> func)
    {
        _pickPhotoFunc = func;
    }

    private Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>>? _navigateToBatchResult;

    public void SetNavigateToBatchResult(Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>> navigate)
    {
        _navigateToBatchResult = navigate;
    }

    [RelayCommand(CanExecute = nameof(CanPickPhoto))]
    private async Task PickAndIdentifyAsync()
    {
        if (_pickPhotoFunc == null) return;

        IsIdentifying = true;
        StatusText = "Picking photo...";

        try
        {
            var imageBytes = await _pickPhotoFunc();
            if (imageBytes == null)
            {
                StatusText = "No photo selected.";
                return;
            }

            StatusText = "Detecting chips...";

            // Run YOLO on the picked image
            var detection = await Task.Run(() => _processor.ProcessFrame(imageBytes));

            if (detection.BoundingBoxes.Count == 0)
            {
                StatusText = "No chips detected in photo. Sending full image...";
                var result = await _identificationService.IdentifyChipAsync(imageBytes);
                if (result.IsSuccess && _navigateToResult != null && result.SvgContent != null)
                {
                    _navigateToResult(result.ChipName ?? "Unknown", result.SvgContent);
                }
                else
                {
                    StatusText = $"Not identified: {result.ErrorMessage}";
                }
                return;
            }

            StatusText = $"Found {detection.BoundingBoxes.Count} chip(s), identifying...";

            // Crop each bounding box and send batch
            var croppedImages = new List<byte[]>();
            using var fullImage = Cv2.ImDecode(imageBytes, ImreadModes.Color);

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
            IdentificationResults = results;

            var identified = results.Where(r => r.IsSuccess).ToList();
            StatusText = $"{identified.Count}/{results.Count} identified";

            // Navigate to result screen with annotated image
            if (_navigateToBatchResult != null)
            {
                _navigateToBatchResult(imageBytes, detection.BoundingBoxes, results);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsIdentifying = false;
        }
    }

    private bool CanPickPhoto() => !_isIdentifying;

    private void OnFrameReady(object? sender, CameraFrameEventArgs e)
    {
        // Display every frame with minimal throttle (smooth preview)
        if (_throttle.ElapsedMilliseconds < 100)
        {
            return;
        }
        _throttle.Restart();

        // Update preview image immediately (no heavy processing)
        try
        {
            using var stream = new MemoryStream(e.ImageBytes);
            var bitmap = new Bitmap(stream);

            Dispatcher.UIThread.Post(() =>
            {
                _lastFrameBytes = e.ImageBytes;
                CameraImage = bitmap;
                RotationDegrees = e.RotationDegrees;
                IdentifyChipsCommand.NotifyCanExecuteChanged();
            });
        }
        catch
        {
            // Ignore display errors
        }

        // Run YOLO detection less frequently (every ~1s) and only if not already processing
        if (_detectionThrottle.ElapsedMilliseconds < 1000)
        {
            return;
        }

        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
        {
            return;
        }

        _detectionThrottle.Restart();
        var frameBytes = e.ImageBytes;

        _ = Task.Run(() =>
        {
            try
            {
                var result = _processor.ProcessFrame(frameBytes);

                Dispatcher.UIThread.Post(() =>
                {
                    BoundingBoxes = result.BoundingBoxes.ToArray();
                    FrameWidth = result.FrameWidth;
                    FrameHeight = result.FrameHeight;
                    StatusText = result.BoundingBoxes.Count == 0 ? "No detections" : $"Detected {result.BoundingBoxes.Count} chip(s)";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => StatusText = $"Error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        });
    }
}
