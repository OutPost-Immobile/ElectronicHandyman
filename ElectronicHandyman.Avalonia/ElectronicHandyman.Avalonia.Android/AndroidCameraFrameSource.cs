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

        var activity = _activity;
        if (activity is not ILifecycleOwner lifecycleOwner)
        {
            _startFailReason = "Activity not set or not ILifecycleOwner";
            return;
        }

        _startFailReason = null;

        var provider = await GetCameraProviderAsync(activity).ConfigureAwait(false);
        var selector = CameraSelector.DefaultBackCamera;

        _analysisExecutor ??= Executors.NewSingleThreadExecutor();
        _imageAnalysis = new ImageAnalysis.Builder()
            .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
            .SetTargetResolution(new Size(1280, 720))
            .Build();

        _imageAnalysis.SetAnalyzer(_analysisExecutor, new FrameAnalyzer(this));

        // BindToLifecycle must run on the main thread
        var bindTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                provider.UnbindAll();
                provider.BindToLifecycle(lifecycleOwner, selector, _imageAnalysis);
                bindTcs.TrySetResult();
            }
            catch (System.Exception ex)
            {
                bindTcs.TrySetException(ex);
            }
        });
        await bindTcs.Task.ConfigureAwait(false);

        _cameraProvider = provider;
        _isRunning = true;
    }

    public string? StartFailReason => _startFailReason;
    private string? _startFailReason;

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
        var tcs = new TaskCompletionSource<ProcessCameraProvider>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        public Size DefaultTargetResolution => new Size(1280, 720);

        public void Analyze(IImageProxy image)
        {
            try
            {
                if (!_owner._isRunning)
                {
                    return;
                }

                var rotationDegrees = image.ImageInfo.RotationDegrees;
                var jpegBytes = ConvertToJpeg(image, out var width, out var height);
                if (jpegBytes.Length > 0)
                {
                    _owner.FrameReady?.Invoke(_owner, new CameraFrameEventArgs(jpegBytes, width, height, rotationDegrees));
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
            var yPlane = planes[0];
            var uPlane = planes[1];
            var vPlane = planes[2];

            int yRowStride = yPlane.RowStride;
            int uvRowStride = uPlane.RowStride;
            int uvPixelStride = uPlane.PixelStride;

            var yBuffer = yPlane.Buffer!;
            var uBuffer = uPlane.Buffer!;
            var vBuffer = vPlane.Buffer!;

            var yData = new byte[yBuffer.Remaining()];
            var uData = new byte[uBuffer.Remaining()];
            var vData = new byte[vBuffer.Remaining()];
            yBuffer.Get(yData);
            uBuffer.Get(uData);
            vBuffer.Get(vData);

            var nv21 = new byte[width * height * 3 / 2];
            int pos = 0;

            for (int row = 0; row < height; row++)
            {
                System.Array.Copy(yData, row * yRowStride, nv21, pos, width);
                pos += width;
            }

            int uvHeight = height / 2;
            int uvWidth = width / 2;

            for (int row = 0; row < uvHeight; row++)
            {
                for (int col = 0; col < uvWidth; col++)
                {
                    int uvIndex = row * uvRowStride + col * uvPixelStride;
                    nv21[pos++] = vData[uvIndex];
                    nv21[pos++] = uData[uvIndex];
                }
            }

            using var yuvImage = new YuvImage(nv21, ImageFormatType.Nv21, width, height, null);
            using var stream = new MemoryStream();
            yuvImage.CompressToJpeg(new Rect(0, 0, width, height), 80, stream);
            return stream.ToArray();
        }
    }
}
