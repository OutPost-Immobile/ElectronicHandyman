using System.Collections.Concurrent;
using EFCore.BulkExtensions;
using ElectronicHandyman.Domain;
using ElectronicHandyman.Domain.Domain.Kicad;
using MSDMarkwort.Kicad.Parser.EESchema;
using MSDMarkwort.Kicad.Parser.EESchema.Models.PartSymbol;
using Services.Abstractions;

namespace Services.Internal;

public class FileDataLoader : IFileDataLoader
{
    private readonly HandymanDbContext _dbContext;

    public FileDataLoader(HandymanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LoadKicadSymFilesAsync(CancellationToken ct = default)
    {
        var directories = Directory.EnumerateDirectories("../kicad-symbols-master-7058584a0fbe9aa2f1c9ff2acf7847726ff6922c");

        var pathList = new List<string>();
        foreach (var directory in directories)
        {
            var files = Directory.EnumerateFiles(directory);

            pathList.AddRange(files);
        }

        var entityList = new ConcurrentBag<SymbolEntity>();
        await Parallel.ForEachAsync(pathList, ct, async (s, token) =>
        {
            var parser = new SymLibParser();
            
            var parserResult = parser.Parse(s);

            if (!parserResult.Success)
            {
                return;
            }

            var symbols = parserResult.Result.Symbols;

            var symbolName = s.Split("/")[3].Replace(".kicad_sym", "");
            
            foreach (var symbol in symbols)
            {
                var extends = symbol.Extends;

                var innerSymbols = symbol.Symbols;
                if (extends != null)
                {
                    var path = pathList.First(x => x.Contains(extends));
                    innerSymbols = await GetExtendedSymCollectionAsync(path, token);
                }
                
                var entity = new SymbolEntity
                {
                    Name = symbolName,
                    Pins = innerSymbols
                        .SelectMany(x => x.Pins)
                        .Select(x => new PinEntity
                        {
                            ElectricalPinType = x.ElectricalPinType,
                            GraphicPinShape = x.GraphicPinShape,
                            Number = x.PinNumber.Name,
                            Name = x.PinName.Name,
                            AtX = x.At.X,
                            AtY = x.At.Y,
                            AtAngle = x.At.Angle,
                        })
                        .ToList(),
                    Polylines = innerSymbols
                        .SelectMany(x => x.Polylines)
                        .Select(x => new PolylineEntity
                        {
                            StrokeWidth = x.Stroke.Width,
                            StrokeType = x.Stroke.Type.ToString(),
                            FillType = x.Fill.Type.ToString(),
                            Points = x.Pts.Positions.Select((y, index) => new PolylinePointEntity
                                {
                                    OrderIndex = index,
                                    X = y.X,
                                    Y = y.Y
                                })
                            .ToList()
                        })
                        .ToList(),
                    Rectangles = innerSymbols.SelectMany(x => x.Rectangles)
                        .Select(x => new RectangleEntity
                        {
                            StartX = x.StartPosition.X,
                            StartY = x.StartPosition.Y,
                            EndX = x.EndPosition.X,
                            EndY = x.EndPosition.Y,
                            StrokeWidth = x.Stroke.Width,
                            StrokeType = x.Stroke.Type.ToString(),
                            FillType = x.Fill.Type.ToString(),
                        })
                        .ToList(),
                    Circles = innerSymbols.SelectMany(x => x.Circles)
                        .Select(x => new CircleEntity
                        {
                            CenterX = x.StartPosition.X,
                            CenterY = x.StartPosition.Y,
                            Radius = x.Radius,
                            StrokeWidth = x.Stroke.Width,
                            StrokeType = x.Stroke.Type.ToString(),
                            FillType = x.Fill.Type.ToString(),
                        })
                        .ToList()
                };
                
                entityList.Add(entity);
            }
        });
        
        _dbContext.Symbols.AddRange(entityList);
        await _dbContext.BulkSaveChangesAsync(cancellationToken: ct);
    }

    private async Task<SymbolCollection> GetExtendedSymCollectionAsync(string boardPath, CancellationToken ct = default)
    {
        var parser = new SymLibParser();
        var parserResult = parser.Parse(boardPath);

        if (!parserResult.Success)
        {
            return [];
        }

        var symbols = parserResult.Result.Symbols;

        return await Task.FromResult(symbols.First().Symbols);
    }
}