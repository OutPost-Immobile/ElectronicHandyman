namespace ElectronicHandyman.Domain.Domain.Kicad;

public class CircleEntity
{
    public int Id { get; set; }
        
    public int SymbolId { get; set; }
    public SymbolEntity Symbol { get; set; }

    public required double CenterX { get; set; }
    public required double CenterY { get; set; }
    public required double Radius { get; set; }

    public required double StrokeWidth { get; set; }
    public required string StrokeType { get; set; }
    public required string FillType { get; set; }
}