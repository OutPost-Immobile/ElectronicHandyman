using System.Globalization;
using System.Xml.Linq;

namespace Services.Internal.Svg.Models;

public record CircleModel
{
    public required double CenterX { get; init; }
    public required double CenterY { get; init; }
    public required double Radius { get; init; }

    public required double StrokeWidth { get; init; }
    public required string StrokeType { get; init; }
    public required string FillType { get; init; }

    public XElement ToXElement()
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var culture = CultureInfo.InvariantCulture;
        
        var svgCenterY = CenterY * -1;
        
        var fillValue = FillType == "background" ? "#ffffe6" : "none";
        
        return new XElement(ns + "circle",
            new XAttribute("cx", CenterX.ToString(culture)),
            new XAttribute("cy", svgCenterY.ToString(culture)),
            new XAttribute("r", Radius.ToString(culture)),
            new XAttribute("fill", fillValue),
            new XAttribute("stroke", "black"), 
            new XAttribute("stroke-width", StrokeWidth.ToString(culture))
        );
    }
}