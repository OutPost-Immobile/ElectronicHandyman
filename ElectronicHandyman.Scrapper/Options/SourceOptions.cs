using System.ComponentModel.DataAnnotations;

namespace ElectronicHandyman.Scrapper.Options;

internal class SourceOptions
{
    [Required]
    public required List<SourceUrl> Items { get; init; }
}

public record SourceUrl
{
    [Required]
    public required string Name { get; init; }
    
    [Required]
    public required string Url { get; init; }
}