namespace ElectronicHandyman.Domain.Domain.Kicad;

public class PinEntity
{
    public int Id { get; set; }
        
    public int SymbolId { get; set; }
    public SymbolEntity Symbol { get; set; }
    
    public required string ElectricalPinType { get; set; }
    public required string GraphicPinShape { get; set; }
    public required string Number { get; set; }
    public required string Name { get; set; }
   
    public required double AtX { get; set; }
    public required double AtY { get; set; }
    public required double AtAngle { get; set; }
}