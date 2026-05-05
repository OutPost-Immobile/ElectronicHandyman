using SkiaSharp;
using Svg.Skia;

namespace Services.Internal;

internal class ImageOverlayService : IImageOverlayService
{
    public byte[] OverlaySvgOnOriginalImage(byte[] originalImageBytes, string svgContent)
    {
        using var baseBitmap = SKBitmap.Decode(originalImageBytes) 
                               ?? throw new InvalidOperationException("Nie udało się zdekodować oryginalnego obrazu wejściowego.");
        
        using var finalImageSurface = SKSurface.Create(new SKImageInfo(baseBitmap.Width, baseBitmap.Height));
        var canvas = finalImageSurface.Canvas;
        
        canvas.DrawBitmap(baseBitmap, 0, 0);
        
        using var svg = new SKSvg();
        using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        
        if (svg.Load(svgStream) is not null)
        {
            float scaleX = (float)baseBitmap.Width / svg.Picture.CullRect.Width;
            float scaleY = (float)baseBitmap.Height / svg.Picture.CullRect.Height;
            float scale = Math.Min(scaleX, scaleY);
            
            var matrix = SKMatrix.CreateScale(scale, scale);
            
            float dx = (baseBitmap.Width - (svg.Picture.CullRect.Width * scale)) / 2;
            float dy = (baseBitmap.Height - (svg.Picture.CullRect.Height * scale)) / 2;
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(dx, dy));
            
            canvas.DrawPicture(svg.Picture, ref matrix);
        }
        
        using var image = finalImageSurface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        
        return data.ToArray();
    }
}