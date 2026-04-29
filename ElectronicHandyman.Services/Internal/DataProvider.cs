using ElectronicHandyman.Domain;
using Services.Abstractions;

namespace Services.Internal;

internal class DataProvider : IDataProvider
{
    private readonly HandymanDbContext _dbContext;

    public DataProvider(HandymanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SearchForArduinoBoardAsync(string boardName)
    {
        
    }

    public async Task SearchForTexasInstrumentsBoardAsync(string boardName)
    {
        
    } 
    
    public async Task SearchForPinoutAsync(string boardName)
    {
        
    }
}