namespace ElectronicHandyman.Domain.Domain.Kicad;

public class PolylinePointEntity
{
    public int Id { get; set; }
        
    public int PolylineId { get; set; }
    public PolylineEntity Polyline { get; set; }
    
    public required int OrderIndex { get; set; } 
    public required double X { get; set; }
    public required double Y { get; set; }
}