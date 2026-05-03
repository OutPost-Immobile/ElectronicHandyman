using Services.Internal.Svg.Models;

namespace Services.Abstractions;

public interface IDataProvider
{
    Task SearchForArduinoBoardAsync(string boardName);
    Task SearchForTexasInstrumentsBoardAsync(string boardName);
    Task<SymbolModel> SearchForPinoutAsync(string boardName);
}