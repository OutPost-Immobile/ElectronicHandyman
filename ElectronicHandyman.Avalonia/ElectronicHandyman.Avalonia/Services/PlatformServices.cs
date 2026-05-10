namespace ElectronicHandyman.Avalonia.Services;

public static class PlatformServices
{
    public static ICameraFrameSource? CameraFrameSource { get; set; }
    public static byte[]? YoloModelBytes { get; set; }
}
