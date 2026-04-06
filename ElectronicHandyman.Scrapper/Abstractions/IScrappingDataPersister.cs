using ElectronicHandyman.Scrapper.Models;

namespace ElectronicHandyman.Scrapper.Abstractions;

public interface IScrappingDataPersister
{
    Task PersistArduinoScrapingData(IEnumerable<BoardFamilyModel> families);
}