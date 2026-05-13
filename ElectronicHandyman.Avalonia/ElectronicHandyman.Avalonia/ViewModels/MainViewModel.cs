using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicHandyman.Avalonia.Services;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElectronicHandyman.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICameraFrameSource _cameraSource;
    private readonly VideoFrameProcessor _processor;
    private readonly Stopwatch _throttle = Stopwatch.StartNew();
    private readonly Stopwatch _detectionThrottle = Stopwatch.StartNew();
    private int _isProcessing;
    
    // Navigation actions
    private Action<string, string>? _navigateToResult;
    private Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>>? _navigateToBatchResult;
    private Action<byte[]>? _navigateToProcessing;

    private Bitmap? _cameraImage;
    private IReadOnlyList<BoundingBox> _boundingBoxes = Array.Empty<BoundingBox>();
    private int _frameWidth;
    private int _frameHeight;
    private int _rotationDegrees;
    private string _statusText = "Waiting for camera...";
    private byte[]? _lastFrameBytes;
    private Func<Task<byte[]?>>? _pickPhotoFunc;

    public MainViewModel() : this(new NoopCameraFrameSource(), new VideoFrameProcessor())
    {
    }

    public MainViewModel(ICameraFrameSource cameraSource, VideoFrameProcessor processor)
    {
        _cameraSource = cameraSource;
        _processor = processor;
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

    public async Task StartAsync()
    {
        try
        {
            StatusText = "Starting camera...";
            await _cameraSource.StartAsync();
            StatusText = _cameraSource.IsRunning ? "Camera running, waiting for frames..." : "Waiting for camera permission...";
        }
        catch (Exception ex)
        {
            StatusText = $"Camera error: {ex.Message}";
        }
    }

    public Task StopAsync() => _cameraSource.StopAsync();

    public void SetNavigateToResult(Action<string, string> navigate) => _navigateToResult = navigate;
    public void SetNavigateToBatchResult(Action<byte[], List<BoundingBox>, List<ChipIdentificationResult>> navigate) => _navigateToBatchResult = navigate;
    public void SetNavigateToProcessing(Action<byte[]> navigate) => _navigateToProcessing = navigate;
    
    public void SetPickPhotoFunc(Func<Task<byte[]?>> func)
    {
        _pickPhotoFunc = func;
        PickAndIdentifyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanIdentify))]
    private void IdentifyChips()
    {
        if (_lastFrameBytes == null)
        {
            StatusText = "No frame available.";
            return;
        }
        _navigateToProcessing?.Invoke(_lastFrameBytes);
    }

    private bool CanIdentify() => _lastFrameBytes != null;

    [RelayCommand(CanExecute = nameof(CanPickPhoto))]
    private async Task PickAndIdentifyAsync()
    {
        if (_pickPhotoFunc == null) return;

        var imageBytes = await _pickPhotoFunc();
        if (imageBytes == null)
        {
            StatusText = "No photo selected.";
            return;
        }
        
        _navigateToProcessing?.Invoke(imageBytes);
    }

    private bool CanPickPhoto() => _pickPhotoFunc != null;

    private void OnFrameReady(object? sender, CameraFrameEventArgs e)
    {
        if (_throttle.ElapsedMilliseconds < 100) return;
        _throttle.Restart();

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
        catch { /* Ignore display errors */ }

        if (_detectionThrottle.ElapsedMilliseconds < 1000 || Interlocked.Exchange(ref _isProcessing, 1) == 1)
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
