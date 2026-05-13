namespace ElectronicHandyman.Domain.Domain.Kicad;

public class SymbolEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public required ICollection<PinEntity> Pins { get; set; } = new List<PinEntity>();
    public required ICollection<PolylineEntity> Polylines { get; set; } = new List<PolylineEntity>();
    public required ICollection<RectangleEntity> Rectangles { get; set; } = new List<RectangleEntity>();
    public required ICollection<CircleEntity> Circles { get; set; } = new List<CircleEntity>();
}