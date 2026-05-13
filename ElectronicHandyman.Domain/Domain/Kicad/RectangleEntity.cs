namespace ElectronicHandyman.Domain.Domain.Kicad;

public class RectangleEntity
{
    public int Id { get; set; }
        
    public int SymbolId { get; set; }
    public SymbolEntity Symbol { get; set; }

    public required double StartX { get; set; }
    public required double StartY { get; set; }
    public required double EndX { get; set; }
    public required double EndY { get; set; }

    public required double StrokeWidth { get; set; }
    public required string StrokeType { get; set; }
    public required string FillType { get; set; }
}