using ElectronicHandyman.Domain.Enums;

namespace ElectronicHandyman.Scrapper.Models;

public class DocumentModel
{
    public required DocumentType DocumentType { get; init; }
    public required string FileName { get; init; }
    public required string StaticUrl { get; init; }
}