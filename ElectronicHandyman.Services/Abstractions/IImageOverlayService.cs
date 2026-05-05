namespace Services;

public interface IImageOverlayService
{
    byte[] OverlaySvgOnOriginalImage(byte[] originalImageBytes, string svgContent);
}