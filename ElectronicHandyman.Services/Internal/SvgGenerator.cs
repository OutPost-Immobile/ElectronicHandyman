using System.Globalization;
using System.Xml.Linq;
using Services.Abstractions;
using Services.Internal.Svg.Models;

namespace Services.Internal;

internal class SvgGenerator : ISvgGenerator
{
    public async Task<XDocument> GenerateSvgDocumentAsync(SymbolModel model)
    {
        // TODO: Add multiple units
        XNamespace ns = "http://www.w3.org/2000/svg";
        var culture = CultureInfo.InvariantCulture;
        
        double minX = 0, maxX = 0, minY = 0, maxY = 0;

        if (model.Rectangles.Any())
        {
            minX = model.Rectangles.Min(r => Math.Min(r.StartX, r.EndX));
            maxX = model.Rectangles.Max(r => Math.Max(r.StartX, r.EndX));
            minY = model.Rectangles.Min(r => Math.Min(r.StartY * -1, r.EndY * -1));
            maxY = model.Rectangles.Max(r => Math.Max(r.StartY * -1, r.EndY * -1));
        }

        if (model.Pins.Any())
        {
            minX = Math.Min(minX, model.Pins.Min(p => p.AtX));
            maxX = Math.Max(maxX, model.Pins.Max(p => p.AtX));
            minY = Math.Min(minY, model.Pins.Min(p => p.AtY * -1));
            maxY = Math.Max(maxY, model.Pins.Max(p => p.AtY * -1));
        }
        
        minX -= 15;
        minY -= 15;
        double width = (maxX - minX) + 30;
        double height = (maxY - minY) + 30;

        var svgElement = new XElement(ns + "svg",
            new XAttribute("viewBox", $"{minX.ToString(culture)} {minY.ToString(culture)} {width.ToString(culture)} {height.ToString(culture)}")
        );
        
        if (model.Polylines.Any())
        {
            foreach (var polyline in model.Polylines)
            {
                svgElement.Add(polyline.ToXElement());
            }
        }
        
        if (model.Rectangles.Any())
        {
            foreach (var rect in model.Rectangles)
            {
                svgElement.Add(rect.ToXElement());
            }
        }
        
        if (model.Circles.Any())
        {
            foreach (var circle in model.Circles)
            {
                svgElement.Add(circle.ToXElement());
            }
        }
        
        if (model.Pins.Any())
        {
            var groupedPins = model.Pins
                .GroupBy(p => new { p.AtX, p.AtY, p.AtAngle });

            foreach (var group in groupedPins)
            {
                var basePin = group.First();
                
                string combinedNumbers = string.Join(", ", group
                    .Select(p => p.Number)
                    .Where(n => !string.IsNullOrEmpty(n)));
                
                svgElement.Add(basePin.ToXElement(combinedNumbers));
            }
        }
        
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            svgElement
        );

        return await Task.FromResult(document);
    }
}