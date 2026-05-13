namespace ElectronicHandyman.App.Services;

/// <summary>
/// Holds identification results for all detected bounding boxes.
/// </summary>
public record IdentificationState
{
    /// <summary>
    /// Per-box identification results. Index corresponds to BoundingBox index.
    /// </summary>
    public List<BoxIdentification> BoxResults { get; init; } = [];

    public bool IsLoading { get; init; }
    public bool IsIdentified => BoxResults.Count > 0 && BoxResults.Any(b => b.IsSuccess);
}

/// <summary>
/// Identification result for a single bounding box.
/// </summary>
public record BoxIdentification
{
    public bool IsSuccess { get; init; }
    public string? SvgContent { get; init; }
    public string? ChipName { get; init; }
    public string? ErrorMessage { get; init; }
    public BoundingBox Box { get; init; }
}
