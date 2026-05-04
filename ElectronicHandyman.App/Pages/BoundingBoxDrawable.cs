using ElectronicHandyman.App.Services;

namespace ElectronicHandyman.App.Pages;

public class BoundingBoxDrawable : IDrawable
{
    public List<BoundingBox> BoundingBoxes { get; set; } = [];
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BoundingBoxes.Count == 0 || FrameWidth == 0 || FrameHeight == 0)
            return;

        float scaleX = dirtyRect.Width / FrameWidth;
        float scaleY = dirtyRect.Height / FrameHeight;

        canvas.StrokeColor = Colors.LimeGreen;
        canvas.StrokeSize = 3;

        foreach (var box in BoundingBoxes)
        {
            float x = box.X * scaleX;
            float y = box.Y * scaleY;
            float w = box.Width * scaleX;
            float h = box.Height * scaleY;

            canvas.DrawRectangle(x, y, w, h);
        }
    }
}
