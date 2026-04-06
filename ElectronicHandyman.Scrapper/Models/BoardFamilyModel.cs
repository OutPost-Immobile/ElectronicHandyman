namespace ElectronicHandyman.Scrapper.Models;

public record BoardFamilyModel
{
    public required string FamilyName { get; init; }
    public required List<BoardModel> Boards { get; init; }
}