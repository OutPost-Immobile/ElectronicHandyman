using ElectronicHandyman.App.Services;

namespace ElectronicHandyman.App.Pages;

public class BoundingBoxDrawable : IDrawable
{
    public List<BoundingBox> BoundingBoxes { get; set; } = [];
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }

    public IdentificationState? IdentificationState { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BoundingBoxes.Count == 0 || FrameWidth == 0 || FrameHeight == 0)
            return;

        float scaleX = dirtyRect.Width / FrameWidth;
        float scaleY = dirtyRect.Height / FrameHeight;

        for (int i = 0; i < BoundingBoxes.Count; i++)
        {
            var box = BoundingBoxes[i];
            float x = box.X * scaleX;
            float y = box.Y * scaleY;
            float w = box.Width * scaleX;
            float h = box.Height * scaleY;

            // Check if this box has been identified
            var boxResult = FindMatchingResult(box);
            bool isIdentified = boxResult is { IsSuccess: true };

            // Outer stroke
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 6;
            canvas.DrawRectangle(x, y, w, h);

            // Inner stroke — gold if identified, lime green otherwise
            canvas.StrokeColor = isIdentified ? Colors.Gold : Colors.LimeGreen;
            canvas.StrokeSize = 3;
            canvas.DrawRectangle(x, y, w, h);

            // Render SVG overlay if identified
            if (isIdentified && !string.IsNullOrEmpty(boxResult!.SvgContent))
            {
                var targetBounds = new RectF(x, y, w, h);
                SvgCanvasRenderer.RenderSvg(canvas, boxResult.SvgContent, targetBounds);
            }

            // Draw chip name label above the bounding box
            if (isIdentified && !string.IsNullOrEmpty(boxResult!.ChipName))
            {
                DrawChipLabel(canvas, boxResult.ChipName, x, y, w);
            }
        }

        // Loading indicator
        if (IdentificationState is { IsLoading: true })
        {
            RenderLoadingIndicator(canvas, scaleX, scaleY);
        }
    }

    private BoxIdentification? FindMatchingResult(BoundingBox currentBox)
    {
        if (IdentificationState?.BoxResults == null || IdentificationState.BoxResults.Count == 0)
            return null;

        // Find the closest matching result by position (tolerance for jitter)
        const int tolerance = 40;
        foreach (var result in IdentificationState.BoxResults)
        {
            if (Math.Abs(result.Box.X - currentBox.X) <= tolerance &&
                Math.Abs(result.Box.Y - currentBox.Y) <= tolerance)
            {
                return result;
            }
        }

        return null;
    }

    private void DrawChipLabel(ICanvas canvas, string chipName, float x, float y, float boxWidth)
    {
        // Background for label
        float labelHeight = 20;
        float labelY = y - labelHeight - 2;
        if (labelY < 0) labelY = y + 2; // below box if no space above

        canvas.FillColor = new Color(0, 0, 0, 180); // semi-transparent black
        canvas.FillRectangle(x, labelY, boxWidth, labelHeight);

        // Text
        canvas.FontSize = 14;
        canvas.FontColor = Colors.White;
        canvas.DrawString(chipName, x + 4, labelY + 2, boxWidth - 8, labelHeight, HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    private void RenderLoadingIndicator(ICanvas canvas, float scaleX, float scaleY)
    {
        if (BoundingBoxes.Count == 0)
            return;

        var box = BoundingBoxes[0];
        float centerX = (box.X + box.Width / 2f) * scaleX;
        float centerY = (box.Y + box.Height / 2f) * scaleY;
        float radius = Math.Min(box.Width * scaleX, box.Height * scaleY) * 0.15f;

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 3;
        canvas.DrawCircle(centerX, centerY, radius);

        canvas.StrokeColor = Colors.Gold;
        canvas.StrokeSize = 4;
        canvas.DrawArc(centerX - radius, centerY - radius,
            radius * 2, radius * 2, 0, 270, false, false);
    }
}
