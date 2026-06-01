using System;
using System.Threading.Tasks;

namespace ElectronicHandyman.Avalonia.Services;

public sealed class NoopCameraFrameSource : ICameraFrameSource
{
    public event EventHandler<CameraFrameEventArgs>? FrameReady;

    public bool IsRunning => false;

    public Task StartAsync() => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;
}
