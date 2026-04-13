using ElectronicHandyman.Scrapper.Models;

namespace ElectronicHandyman.Scrapper.Abstractions;

public interface ITexasService
{
    Task<IEnumerable<BoardFamilyModel>> AuthenticateAndGetBoardsFamily(string boardFamilyName);
    Task<Stream> GetBoardDocumentAsync(string boardFamilyName);
}