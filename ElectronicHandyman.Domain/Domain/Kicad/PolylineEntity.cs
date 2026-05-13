namespace ElectronicHandyman.Domain.Domain.Kicad;

public class PolylineEntity
{
    public int Id { get; set; }
        
    public int SymbolId { get; set; }
    public SymbolEntity Symbol { get; set; }

    public required double StrokeWidth { get; set; }
    public required string StrokeType { get; set; }
    public required string FillType { get; set; }

    public required ICollection<PolylinePointEntity> Points { get; set; }   
}