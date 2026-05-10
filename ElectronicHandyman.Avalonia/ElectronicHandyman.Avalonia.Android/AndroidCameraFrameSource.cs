using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using ElectronicHandyman.Avalonia.Services;
using Java.Lang;
using Java.Util.Concurrent;
using Exception = Java.Lang.Exception;

namespace ElectronicHandyman.Avalonia.Android;

public sealed class AndroidCameraFrameSource : Java.Lang.Object, ICameraFrameSource
{
    private readonly object _sync = new();
    private Activity? _activity;
    private ProcessCameraProvider? _cameraProvider;
    private ImageAnalysis? _imageAnalysis;
    private IExecutorService? _analysisExecutor;
    private bool _isRunning;

    public event EventHandler<CameraFrameEventArgs>? FrameReady;

    public bool IsRunning => _isRunning;

    public void SetActivity(Activity activity)
    {
        _activity = activity;
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            return;
        }

        if (_activity is not ILifecycleOwner lifecycleOwner)
        {
            return;
        }

        var provider = await GetCameraProviderAsync(_activity);
        var selector = CameraSelector.DefaultBackCamera;

        _analysisExecutor ??= Executors.NewSingleThreadExecutor();
        _imageAnalysis = new ImageAnalysis.Builder()
            .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
            .SetTargetResolution(new Size(1280, 720))
            .Build();

        _imageAnalysis.SetAnalyzer(_analysisExecutor, new FrameAnalyzer(this));

        provider.UnbindAll();
        provider.BindToLifecycle(lifecycleOwner, selector, _imageAnalysis);

        _cameraProvider = provider;
        _isRunning = true;
    }

    public Task StopAsync()
    {
        lock (_sync)
        {
            _isRunning = false;
            _imageAnalysis?.ClearAnalyzer();
            _cameraProvider?.UnbindAll();
            _analysisExecutor?.Shutdown();
            _analysisExecutor = null;
        }

        return Task.CompletedTask;
    }

    private static Task<ProcessCameraProvider> GetCameraProviderAsync(Activity activity)
    {
        var tcs = new TaskCompletionSource<ProcessCameraProvider>();
        var future = ProcessCameraProvider.GetInstance(activity);
        future.AddListener(new Runnable(() =>
        {
            try
            {
                tcs.TrySetResult((ProcessCameraProvider)future.Get());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }), ContextCompat.GetMainExecutor(activity));
        return tcs.Task;
    }

    private sealed class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly AndroidCameraFrameSource _owner;

        public FrameAnalyzer(AndroidCameraFrameSource owner)
        {
            _owner = owner;
        }

        public void Analyze(IImageProxy image)
        {
            try
            {
                if (!_owner._isRunning)
                {
                    return;
                }

                var jpegBytes = ConvertToJpeg(image, out var width, out var height);
                if (jpegBytes.Length > 0)
                {
                    _owner.FrameReady?.Invoke(_owner, new CameraFrameEventArgs(jpegBytes, width, height));
                }
            }
            catch
            {
                // Ignore per-frame errors to keep the camera running.
            }
            finally
            {
                image.Close();
            }
        }

        private static byte[] ConvertToJpeg(IImageProxy image, out int width, out int height)
        {
            width = image.Width;
            height = image.Height;

            var planes = image.GetPlanes();
            var yBuffer = planes[0].Buffer;
            var uBuffer = planes[1].Buffer;
            var vBuffer = planes[2].Buffer;

            int ySize = yBuffer.Remaining();
            int uSize = uBuffer.Remaining();
            int vSize = vBuffer.Remaining();

            var nv21 = new byte[ySize + uSize + vSize];
            yBuffer.Get(nv21, 0, ySize);
            vBuffer.Get(nv21, ySize, vSize);
            uBuffer.Get(nv21, ySize + vSize, uSize);

            using var yuvImage = new YuvImage(nv21, ImageFormatType.Nv21, width, height, null);
            using var stream = new MemoryStream();
            yuvImage.CompressToJpeg(new Rect(0, 0, width, height), 80, stream);
            return stream.ToArray();
        }
    }
}
