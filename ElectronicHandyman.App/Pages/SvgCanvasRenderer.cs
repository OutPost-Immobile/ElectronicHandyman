using System.Xml.Linq;

namespace ElectronicHandyman.App.Pages;

/// <summary>
/// Parses SVG content and renders it onto a MAUI ICanvas within the specified bounds.
/// Supports: rect, line, polyline, circle, text elements.
/// </summary>
public static class SvgCanvasRenderer
{
    /// <summary>
    /// Renders SVG content onto the given canvas, scaled to fit within targetBounds
    /// while preserving the original aspect ratio.
    /// </summary>
    public static void RenderSvg(ICanvas canvas, string svgContent, RectF targetBounds)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            return;

        try
        {
            var doc = XDocument.Parse(svgContent);
            var root = doc.Root;
            if (root == null)
                return;

            // Extract viewBox for coordinate mapping
            var viewBox = ParseViewBox(root);
            if (viewBox.Width <= 0 || viewBox.Height <= 0)
                return;

            // Scale SVG to fill the entire target bounding box
            // Use targetBounds directly as render area
            float scaleX = targetBounds.Width / viewBox.Width;
            float scaleY = targetBounds.Height / viewBox.Height;
            
            // Use uniform scale (the larger one) to make SVG bigger and fill the box
            float uniformScale = Math.Max(scaleX, scaleY) * 5f; // 5x multiplier for visibility
            scaleX = uniformScale;
            scaleY = uniformScale;
            
            // viewBox origin — must be subtracted from SVG coordinates before scaling
            float vbX = viewBox.X;
            float vbY = viewBox.Y;
            
            // Center of the bounding box
            float boxCenterX = targetBounds.X + targetBounds.Width / 2f;
            float boxCenterY = targetBounds.Y + targetBounds.Height / 2f;
            
            // Center of the SVG viewBox (in viewBox coordinates)
            float svgCenterX = vbX + viewBox.Width / 2f;
            float svgCenterY = vbY + viewBox.Height / 2f;
            
            // Offset so that SVG center maps to bounding box center
            float offsetX = boxCenterX - (svgCenterX - vbX) * scaleX;
            float offsetY = boxCenterY - (svgCenterY - vbY) * scaleY;

            System.Diagnostics.Debug.WriteLine($"[SVG] viewBox=({vbX},{vbY},{viewBox.Width},{viewBox.Height}), target=({targetBounds.X},{targetBounds.Y},{targetBounds.Width},{targetBounds.Height}), scale={uniformScale:F3}");

            int elementCount = 0;

            // Render each supported SVG element
            foreach (var element in root.Descendants())
            {
                try
                {
                    var localName = element.Name.LocalName.ToLowerInvariant();
                    switch (localName)
                    {
                        case "rect":
                            RenderRect(canvas, element, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
                            elementCount++;
                            break;
                        case "line":
                            RenderLine(canvas, element, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
                            elementCount++;
                            break;
                        case "polyline":
                            RenderPolyline(canvas, element, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
                            elementCount++;
                            break;
                        case "circle":
                            RenderCircle(canvas, element, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
                            elementCount++;
                            break;
                        case "text":
                            RenderText(canvas, element, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
                            elementCount++;
                            break;
                    }
                }
                catch (Exception)
                {
                    // Skip malformed individual elements
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SVG] Rendered {elementCount} elements");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SvgCanvasRenderer: Failed to render SVG - {ex.Message}");
        }
    }

    internal static RectF ComputeScaledRect(RectF viewBox, RectF targetBounds)
    {
        // Fill the entire bounding box — stretch SVG to fit
        return targetBounds;
    }

    private static RectF ParseViewBox(XElement root)
    {
        var viewBoxAttr = root.Attribute("viewBox")?.Value;
        if (string.IsNullOrWhiteSpace(viewBoxAttr))
        {
            float w = GetFloatAttr(root, "width");
            float h = GetFloatAttr(root, "height");
            return new RectF(0, 0, w, h);
        }

        var parts = viewBoxAttr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            return RectF.Zero;

        if (float.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(parts[2], System.Globalization.CultureInfo.InvariantCulture, out float width) &&
            float.TryParse(parts[3], System.Globalization.CultureInfo.InvariantCulture, out float height))
        {
            return new RectF(x, y, width, height);
        }

        return RectF.Zero;
    }

    private static void RenderRect(ICanvas canvas, XElement element, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        float x = (GetFloatAttr(element, "x") - vbX) * scaleX + offsetX;
        float y = (GetFloatAttr(element, "y") - vbY) * scaleY + offsetY;
        float w = GetFloatAttr(element, "width") * scaleX;
        float h = GetFloatAttr(element, "height") * scaleY;

        ApplyFill(canvas, element);
        if (HasFill(element))
        {
            canvas.FillRectangle(x, y, w, h);
        }

        ApplyStroke(canvas, element);
        if (HasStroke(element))
        {
            canvas.DrawRectangle(x, y, w, h);
        }
    }

    private static void RenderLine(ICanvas canvas, XElement element, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        float x1 = (GetFloatAttr(element, "x1") - vbX) * scaleX + offsetX;
        float y1 = (GetFloatAttr(element, "y1") - vbY) * scaleY + offsetY;
        float x2 = (GetFloatAttr(element, "x2") - vbX) * scaleX + offsetX;
        float y2 = (GetFloatAttr(element, "y2") - vbY) * scaleY + offsetY;

        ApplyStroke(canvas, element);
        canvas.DrawLine(x1, y1, x2, y2);
    }

    private static void RenderPolyline(ICanvas canvas, XElement element, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        var pointsAttr = element.Attribute("points")?.Value;
        if (string.IsNullOrWhiteSpace(pointsAttr))
            return;

        var points = ParsePoints(pointsAttr, scaleX, scaleY, offsetX, offsetY, vbX, vbY);
        if (points.Count < 2)
            return;

        ApplyStroke(canvas, element);

        for (int i = 0; i < points.Count - 1; i++)
        {
            canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y);
        }
    }

    private static void RenderCircle(ICanvas canvas, XElement element, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        float cx = (GetFloatAttr(element, "cx") - vbX) * scaleX + offsetX;
        float cy = (GetFloatAttr(element, "cy") - vbY) * scaleY + offsetY;
        float r = GetFloatAttr(element, "r") * Math.Min(scaleX, scaleY);

        ApplyFill(canvas, element);
        if (HasFill(element))
        {
            canvas.FillCircle(cx, cy, r);
        }

        ApplyStroke(canvas, element);
        if (HasStroke(element))
        {
            canvas.DrawCircle(cx, cy, r);
        }
    }

    private static void RenderText(ICanvas canvas, XElement element, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        float x = (GetFloatAttr(element, "x") - vbX) * scaleX + offsetX;
        float y = (GetFloatAttr(element, "y") - vbY) * scaleY + offsetY;
        string text = element.Value;

        if (string.IsNullOrWhiteSpace(text))
            return;

        // Large, bold font for visibility
        canvas.FontSize = 12;
        canvas.FontColor = Colors.Cyan;
        canvas.DrawString(text, x, y, HorizontalAlignment.Left);
    }

    private static List<PointF> ParsePoints(string pointsAttr, float scaleX, float scaleY, float offsetX, float offsetY, float vbX, float vbY)
    {
        var result = new List<PointF>();
        var pairs = pointsAttr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var coords = pair.Split(',');
            if (coords.Length == 2 &&
                float.TryParse(coords[0], System.Globalization.CultureInfo.InvariantCulture, out float px) &&
                float.TryParse(coords[1], System.Globalization.CultureInfo.InvariantCulture, out float py))
            {
                result.Add(new PointF((px - vbX) * scaleX + offsetX, (py - vbY) * scaleY + offsetY));
            }
        }

        return result;
    }

    private static void ApplyStroke(ICanvas canvas, XElement element)
    {
        // All strokes in bright red for maximum visibility on camera feed
        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 4; // fixed thick stroke for visibility
    }

    private static void ApplyFill(ICanvas canvas, XElement element)
    {
        var fill = element.Attribute("fill")?.Value;
        if (!string.IsNullOrWhiteSpace(fill) && fill != "none")
        {
            canvas.FillColor = ParseColor(fill);
        }
    }

    private static bool HasStroke(XElement element)
    {
        var stroke = element.Attribute("stroke")?.Value;
        return !string.IsNullOrWhiteSpace(stroke) && stroke != "none";
    }

    private static bool HasFill(XElement element)
    {
        var fill = element.Attribute("fill")?.Value;
        return !string.IsNullOrWhiteSpace(fill) && fill != "none";
    }

    private static Color ParseColor(string colorStr)
    {
        if (string.IsNullOrWhiteSpace(colorStr))
            return Colors.White;

        try
        {
            return Color.Parse(colorStr);
        }
        catch
        {
            return Colors.White;
        }
    }

    private static float GetFloatAttr(XElement element, string name, float defaultValue = 0f)
    {
        var attr = element.Attribute(name)?.Value;
        if (string.IsNullOrWhiteSpace(attr))
            return defaultValue;

        // Strip units like "px", "pt"
        attr = attr.TrimEnd('p', 'x', 't', 'e', 'm');

        if (float.TryParse(attr, System.Globalization.CultureInfo.InvariantCulture, out float value))
            return value;

        return defaultValue;
    }
}
