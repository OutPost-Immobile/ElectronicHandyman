namespace ElectronicHandyman.App.Services;

public record IdentificationResult
{
    public bool IsSuccess { get; init; }
    public string? SvgContent { get; init; }
    public string? ChipName { get; init; }
    public string? ErrorMessage { get; init; }
}
