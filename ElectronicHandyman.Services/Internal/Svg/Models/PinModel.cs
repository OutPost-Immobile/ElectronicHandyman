using System.Globalization;
using System.Xml.Linq;

namespace Services.Internal.Svg.Models;

public record PinModel
{
    public required string ElectricalPinType { get; init; }
    public required string GraphicPinShape { get; init; }
    public required string Number { get; init; }
    public required string Name { get; init; }
    
    public required double AtX { get; init; }
    public required double AtY { get; init; }
    public required double AtAngle { get; init; }

    public XElement ToXElement(string? numberToDraw = null)
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var culture = CultureInfo.InvariantCulture;

        var constLength = 2.54;
        
        var startX = AtX;
        var startY = AtY * -1;
        var angleInRadians = AtAngle * (Math.PI / 180.0);
        
        double endX = Math.Round(startX + constLength * Math.Cos(angleInRadians), 4);
        double endY = Math.Round(startY - constLength * Math.Sin(angleInRadians), 4);
        
        var pinGroup = new XElement(ns + "g", new XAttribute("class", "kicad-pin"));
        
        pinGroup.Add(new XElement(ns + "line",
            new XAttribute("x1", startX.ToString("0.####", culture)),
            new XAttribute("y1", startY.ToString("0.####", culture)),
            new XAttribute("x2", endX.ToString("0.####", culture)),
            new XAttribute("y2", endY.ToString("0.####", culture)),
            new XAttribute("stroke", "darkred"),
            new XAttribute("stroke-width", "0.254") 
        ));
        
        if (!string.IsNullOrEmpty(Number))
        {
            double midX = startX + (constLength / 2) * Math.Cos(angleInRadians);
            double midY = startY - (constLength / 2) * Math.Sin(angleInRadians);

            bool isVertical = Math.Abs(Math.Cos(angleInRadians)) < 0.01;

            double numX = Math.Round(midX + (isVertical ? 0.5 : -0.5), 4);
            double numY = Math.Round(midY + (isVertical ? 0.0 : -0.5), 4);

            var numberElement = new XElement(ns + "text",
                new XAttribute("x", numX.ToString("0.####", culture)),
                new XAttribute("y", numY.ToString("0.####", culture)),
                new XAttribute("font-size", "1.27"),
                new XAttribute("font-family", "sans-serif"),
                new XAttribute("fill", "darkred"),
                numberToDraw ?? Number
            );

            if (isVertical)
            {
                numberElement.Add(new XAttribute("dominant-baseline", "middle"));
            }

            pinGroup.Add(numberElement);
        }

        if (!string.IsNullOrEmpty(Name) && Name != "~")
        {
            pinGroup.Add(new XElement(ns + "text",
                new XAttribute("x", endX.ToString("0.####", culture)),
                new XAttribute("y", endY.ToString("0.####", culture)),
                new XAttribute("font-size", "1.27"),
                new XAttribute("font-family", "sans-serif"),
                new XAttribute("fill", "teal"),
                new XAttribute("dominant-baseline", "middle"),
                new XAttribute("text-anchor", GetTextAnchor(AtAngle)),
                Name
            ));
        }

        return pinGroup;
    }
    
    private static string GetTextAnchor(double angle)
    {
        return angle switch
        {
            0 => "start",      
            180 => "end",      
            _ => "middle"
        };
    }
}