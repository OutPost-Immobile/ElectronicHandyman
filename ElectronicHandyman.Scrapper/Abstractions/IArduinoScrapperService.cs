using ElectronicHandyman.Scrapper.Models;
using ElectronicHandyman.Scrapper.Models.Api;

namespace ElectronicHandyman.Scrapper.Abstractions;

public interface IArduinoScrapperService
{
    Task<IEnumerable<BoardFamilyModel>> ScrapArduinoBoardsPageAsync();
    Task<DataModel> ScrapFromJsonAsync(string boardUrl, string boardName);
}