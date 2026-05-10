using System;

namespace ElectronicHandyman.Avalonia.Services;

public sealed class CameraFrameEventArgs : EventArgs
{
    public CameraFrameEventArgs(byte[] imageBytes, int width, int height)
    {
        ImageBytes = imageBytes;
        Width = width;
        Height = height;
    }

    public byte[] ImageBytes { get; }
    public int Width { get; }
    public int Height { get; }
}
