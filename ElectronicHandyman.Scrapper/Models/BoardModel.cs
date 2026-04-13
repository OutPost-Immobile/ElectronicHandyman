namespace ElectronicHandyman.Scrapper.Models;

public record BoardModel
{
    public required string Name { get; init; }
    public required string Href { get; init; }
    
    public required List<DocumentModel> Documents { get; init; }
}