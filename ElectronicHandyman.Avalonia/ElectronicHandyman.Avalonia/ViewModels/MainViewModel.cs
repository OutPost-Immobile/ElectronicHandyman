using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private int _isProcessing;

    private Bitmap? _cameraImage;
    private IReadOnlyList<BoundingBox> _boundingBoxes = Array.Empty<BoundingBox>();
    private int _frameWidth;
    private int _frameHeight;
    private string _statusText = "Waiting for camera...";

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

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public Task StartAsync() => _cameraSource.StartAsync();

    public Task StopAsync() => _cameraSource.StopAsync();

    private void OnFrameReady(object? sender, CameraFrameEventArgs e)
    {
        if (_throttle.ElapsedMilliseconds < 200)
        {
            return;
        }

        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
        {
            return;
        }

        _throttle.Restart();

        _ = Task.Run(() =>
        {
            try
            {
                var result = _processor.ProcessFrame(e.ImageBytes);
                using var stream = new MemoryStream(e.ImageBytes);
                var bitmap = new Bitmap(stream);

                Dispatcher.UIThread.Post(() =>
                {
                    CameraImage = bitmap;
                    BoundingBoxes = result.BoundingBoxes.ToArray();
                    FrameWidth = result.FrameWidth;
                    FrameHeight = result.FrameHeight;
                    StatusText = result.BoundingBoxes.Count == 0 ? "No detections" : "Detecting...";
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
