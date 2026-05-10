using System;
using System.Threading.Tasks;

namespace ElectronicHandyman.Avalonia.Services;

public interface ICameraFrameSource
{
    event EventHandler<CameraFrameEventArgs>? FrameReady;
    bool IsRunning { get; }
    Task StartAsync();
    Task StopAsync();
}
