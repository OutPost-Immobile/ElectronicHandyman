namespace Services.Internal.Svg.Models;

public record SymbolModel
{
    public required string Name { get; init; }
    public required IEnumerable<PinModel> Pins { get; init; }
    public required IEnumerable<CircleModel> Circles { get; init; }
    public required IEnumerable<RectangleModel> Rectangles { get; init; }
    public required IEnumerable<PolylineModel> Polylines { get; init; }
}