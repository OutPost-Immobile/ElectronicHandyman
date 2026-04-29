namespace Services.Abstractions;

public interface IDataProvider
{
    Task SearchForArduinoBoardAsync(string boardName);
    Task SearchForTexasInstrumentsBoardAsync(string boardName);
    Task SearchForPinoutAsync(string boardName);
}