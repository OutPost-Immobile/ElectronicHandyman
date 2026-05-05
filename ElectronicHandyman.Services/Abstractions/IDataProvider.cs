using Services.Internal;
using Services.Internal.Svg.Models;

namespace Services.Abstractions;

public interface IDataProvider
{
    Task SearchForArduinoBoardAsync(string boardName);
    Task SearchForTexasInstrumentsBoardAsync(string boardName);
    Task<SymbolModel> SearchForPinoutAsync(string boardName);
    Task<SearchResult> SearchForPinoutFuzzyAsync(string normalizedOcrText, double threshold = 6.0);
}