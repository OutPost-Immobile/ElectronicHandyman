using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ElectronicHandyman.Avalonia.Services;

namespace ElectronicHandyman.Avalonia.Controls;

public class BoundingBoxOverlay : Control
{
    public static readonly StyledProperty<IReadOnlyList<BoundingBox>?> BoxesProperty =
        AvaloniaProperty.Register<BoundingBoxOverlay, IReadOnlyList<BoundingBox>?>(nameof(Boxes));

    public static readonly StyledProperty<int> FrameWidthProperty =
        AvaloniaProperty.Register<BoundingBoxOverlay, int>(nameof(FrameWidth));

    public static readonly StyledProperty<int> FrameHeightProperty =
        AvaloniaProperty.Register<BoundingBoxOverlay, int>(nameof(FrameHeight));

    static BoundingBoxOverlay()
    {
        AffectsRender<BoundingBoxOverlay>(BoxesProperty, FrameWidthProperty, FrameHeightProperty);
    }

    public IReadOnlyList<BoundingBox>? Boxes
    {
        get => GetValue(BoxesProperty);
        set => SetValue(BoxesProperty, value);
    }

    public int FrameWidth
    {
        get => GetValue(FrameWidthProperty);
        set => SetValue(FrameWidthProperty, value);
    }

    public int FrameHeight
    {
        get => GetValue(FrameHeightProperty);
        set => SetValue(FrameHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Boxes is null || Boxes.Count == 0 || FrameWidth <= 0 || FrameHeight <= 0)
        {
            return;
        }

        double scaleX = Bounds.Width / FrameWidth;
        double scaleY = Bounds.Height / FrameHeight;

        var outerPen = new Pen(Brushes.Black, 4);
        var innerPen = new Pen(Brushes.LimeGreen, 2);
        var labelBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));

        foreach (var box in Boxes)
        {
            var rect = new Rect(
                box.X * scaleX,
                box.Y * scaleY,
                box.Width * scaleX,
                box.Height * scaleY);

            context.DrawRectangle(null, outerPen, rect);
            context.DrawRectangle(null, innerPen, rect);

            // Draw confidence label
            if (box.Confidence > 0)
            {
                var text = new FormattedText(
                    $"{box.Confidence:P0}",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                    11,
                    Brushes.LimeGreen);

                double labelX = rect.X;
                double labelY = rect.Y - text.Height - 2;
                if (labelY < 0) labelY = rect.Y + 2;

                // Background for label
                var labelRect = new Rect(labelX, labelY, text.Width + 6, text.Height + 2);
                context.DrawRectangle(labelBrush, null, labelRect);
                context.DrawText(text, new Point(labelX + 3, labelY + 1));
            }
        }
    }
}
