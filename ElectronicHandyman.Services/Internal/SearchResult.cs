using Services.Internal.Svg.Models;

namespace Services.Internal;

public enum MatchType
{
    Exact,
    Fuzzy,
    NotFound
}

public record SearchResult
{
    public required MatchType Type { get; init; }
    public SymbolModel? Symbol { get; init; }
    public double Distance { get; init; }
    public string? SearchedText { get; init; }
    public string? ErrorMessage { get; init; }
}

public record MatchResult(string MatchedName, double Distance);
