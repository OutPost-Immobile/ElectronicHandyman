using System;

namespace ElectronicHandyman.Avalonia.Services;

public sealed class CameraFrameEventArgs : EventArgs
{
    public CameraFrameEventArgs(byte[] imageBytes, int width, int height, int rotationDegrees = 0)
    {
        ImageBytes = imageBytes;
        Width = width;
        Height = height;
        RotationDegrees = rotationDegrees;
    }

    public byte[] ImageBytes { get; }
    public int Width { get; }
    public int Height { get; }
    public int RotationDegrees { get; }
}
