using ElectronicHandyman.Domain;
using ElectronicHandyman.Scrapper.Abstractions;
using Microsoft.EntityFrameworkCore;
using Services.Abstractions;
using Services.Internal.Svg.Models;

namespace Services.Internal;

internal class DataProvider : IDataProvider
{
    private readonly HandymanDbContext _dbContext;
    private readonly ITexasService _texasService;

    public DataProvider(HandymanDbContext dbContext, ITexasService texasService)
    {
        _dbContext = dbContext;
        _texasService = texasService;
    }

    public async Task SearchForArduinoBoardAsync(string boardName)
    {
        
    }

    public async Task SearchForTexasInstrumentsBoardAsync(string boardName)
    {
        var boards = await _texasService.AuthenticateAndGetBoardsFamily(boardName);
    } 
    
    public async Task<SymbolModel> SearchForPinoutAsync(string boardName)
    {
        var pinout = await _dbContext.Symbols
            .Where(x => x.Name.ToLower() == boardName.ToLower())
            .Select(x => new SymbolModel
            {
                Name = x.Name,
                Pins = x.Pins
                    .OrderBy(o => o.Number)
                    .Select(p => new PinModel
                    {
                        ElectricalPinType = p.ElectricalPinType,
                        GraphicPinShape = p.GraphicPinShape,
                        Number = p.Number,
                        Name = p.Name,
                        AtX = p.AtX,
                        AtY = p.AtY,
                        AtAngle = p.AtAngle,
                    }),
                Circles = x.Circles.Select(c => new CircleModel
                {
                    CenterX = c.CenterX,
                    CenterY = c.CenterY,
                    Radius = c.Radius,
                    StrokeWidth = c.StrokeWidth,
                    StrokeType = c.StrokeType,
                    FillType = c.FillType
                }),
                Rectangles = x.Rectangles.Select(r => new RectangleModel
                {
                    StartX = r.StartX,
                    StartY = r.StartY,
                    EndX = r.EndX,
                    EndY = r.EndY,
                    StrokeWidth = r.StrokeWidth,
                    StrokeType = r.StrokeType,
                    FillType = r.FillType
                }),
                Polylines = x.Polylines.Select(p => new PolylineModel
                {
                    StrokeWidth = p.StrokeWidth,
                    StrokeType = p.StrokeType,
                    FillType = p.FillType,
                    Points = p.Points
                        .OrderBy(o => o.OrderIndex)
                        .Select(y => new PolylineModel.PointModel
                        {
                            OrderIndex = y.OrderIndex,
                            X = y.X,
                            Y = y.Y
                        })
                })
            })
            .FirstOrDefaultAsync();
        
        return pinout ?? throw new KeyNotFoundException();
    }
}