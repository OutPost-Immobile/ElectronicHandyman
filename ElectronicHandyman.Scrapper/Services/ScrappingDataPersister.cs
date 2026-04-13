using ElectronicHandyman.Domain;
using ElectronicHandyman.Domain.Domain;
using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Models;

namespace ElectronicHandyman.Scrapper.Services;

internal class ScrappingDataPersister : IScrappingDataPersister
{
    private readonly HandymanDbContext _dbContext;

    public ScrappingDataPersister(HandymanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PersistArduinoScrapingData(IEnumerable<BoardFamilyModel> families)
    {
        var entities = families
            .Select(x => new BoardFamilyEntity
            {
                FamilyName = x.FamilyName,
                Boards = x.Boards.Select(b => new BoardEntity
                    {
                        ModelName = b.Name,
                        Href = b.Href,
                        Documents = b.Documents.Select(d => new BoardDocumentEntity
                        {
                            DocumentType = d.DocumentType,
                            FileName = d.FileName,
                            StaticUrl = d.StaticUrl
                        })
                        .ToList()
                    })
                    .ToList()
            });
        
        _dbContext.AddRange(entities);
        await _dbContext.SaveChangesAsync();
    }
}