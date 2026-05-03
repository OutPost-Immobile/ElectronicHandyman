using System.Globalization;
using System.Xml.Linq;

namespace Services.Internal.Svg.Models;

public record RectangleModel
{
    public required double StartX { get; init; }
    public required double StartY { get; init; }
    public required double EndX { get; init; }
    public required double EndY { get; init; }

    public required double StrokeWidth { get; init; }
    public required string StrokeType { get; init; }
    public required string FillType { get; init; }

    public XElement ToXElement()
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var culture = CultureInfo.InvariantCulture;
        
        var svgStartY = StartY * -1;
        var svgEndY = EndY * -1;
        
        var x = Math.Min(StartX, EndX);
        var y = Math.Min(svgStartY, svgEndY);
        var width = Math.Abs(StartX - EndX);
        var height = Math.Abs(svgStartY - svgEndY);

        var fillValue = FillType == "background" ? "#ffffe6" : "none";
        
        return new XElement(ns + "rect",
            new XAttribute("x", x.ToString(culture)),
            new XAttribute("y", y.ToString(culture)),
            new XAttribute("width", width.ToString(culture)),
            new XAttribute("height", height.ToString(culture)),
            new XAttribute("fill", fillValue),
            new XAttribute("stroke", "black"),
            new XAttribute("stroke-width", StrokeWidth.ToString(culture))
        );
    }
}