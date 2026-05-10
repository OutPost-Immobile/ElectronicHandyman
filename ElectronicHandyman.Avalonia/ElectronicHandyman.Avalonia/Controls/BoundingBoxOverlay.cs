using System.Collections.Generic;
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

        foreach (var box in Boxes)
        {
            var rect = new Rect(
                box.X * scaleX,
                box.Y * scaleY,
                box.Width * scaleX,
                box.Height * scaleY);

            context.DrawRectangle(null, outerPen, rect);
            context.DrawRectangle(null, innerPen, rect);
        }
    }
}
