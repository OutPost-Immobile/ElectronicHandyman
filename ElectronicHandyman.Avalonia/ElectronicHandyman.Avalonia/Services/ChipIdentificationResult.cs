namespace ElectronicHandyman.Avalonia.Services;

public record ChipIdentificationResult
{
    public bool IsSuccess { get; init; }
    public string? SvgContent { get; init; }
    public string? ChipName { get; init; }
    public string? ErrorMessage { get; init; }
}
