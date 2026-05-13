using System.Globalization;
using System.Xml.Linq;

namespace Services.Internal.Svg.Models;

public record PolylineModel
{
    public required double StrokeWidth { get; init; }
    public required string StrokeType { get; init; }
    public required string FillType { get; init; }
    public required IEnumerable<PointModel> Points { get; set; }
    
    public record PointModel
    {
        public required int OrderIndex { get; init; } 
        public required double X { get; init; }
        public required double Y { get; init; }
    }

    public XElement ToXElement()
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var culture = CultureInfo.InvariantCulture;
        
        var orderedPoints = Points.OrderBy(p => p.OrderIndex);
        
        var pointsString = string.Join(" ", orderedPoints.Select(p => 
            $"{p.X.ToString(culture)},{(p.Y * -1).ToString(culture)}"));

        var fillValue = FillType == "background" ? "#ffffe6" : "none";
        
        return new XElement(ns + "polyline",
            new XAttribute("points", pointsString),
            new XAttribute("fill", fillValue),
            new XAttribute("stroke", "black"),
            new XAttribute("stroke-width", StrokeWidth.ToString(culture))
        );
    }
}