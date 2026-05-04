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
    
    private readonly LevenshteinMatcher _matcher = new();

    public async Task<SearchResult> SearchForPinoutFuzzyAsync(string normalizedOcrText, double threshold = 6.0)
    {
        var exactMatch = await _dbContext.Symbols
            .Where(x => x.Name.ToLower() == normalizedOcrText.ToLower())
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
        
        if (exactMatch is not null)
        {
            return new SearchResult
            {
                Type = MatchType.Exact,
                Symbol = exactMatch,
                Distance = 0,
                SearchedText = normalizedOcrText
            };
        }
        
        Console.WriteLine($"Starting fuzzy search for [{normalizedOcrText}]");
        var candidateNames = await _dbContext.Symbols
            .Select(x => x.Name)
            .ToListAsync();
        Console.WriteLine($"Loaded {candidateNames.Count} candidates");

        // Step 7 & 8: Compute weighted Levenshtein and find best match
        // Normalize candidate names to uppercase for fair comparison (DB may have mixed case like "STM32F103C6Tx")
        var normalizedCandidates = candidateNames
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => (Original: c, Upper: c.ToUpperInvariant()))
            .ToList();

        Console.WriteLine($"Fuzzy search: query=[{normalizedOcrText}], candidates={normalizedCandidates.Count}, threshold={threshold}");
        foreach (var (orig, upper) in normalizedCandidates.Take(10))
        {
            var d = _matcher.ComputeDistance(normalizedOcrText, upper);
            Console.WriteLine($"  Candidate [{orig}] (upper=[{upper}]) distance={d:F1}");
        }

        // Find best match using uppercase names
        string? bestOriginalName = null;
        double bestDistance = double.MaxValue;

        foreach (var (orig, upper) in normalizedCandidates)
        {
            var distance = _matcher.ComputeDistance(normalizedOcrText, upper);
            if (distance < bestDistance ||
                (distance == bestDistance && bestOriginalName != null && orig.Length < bestOriginalName.Length))
            {
                bestDistance = distance;
                bestOriginalName = orig;
            }
        }

        MatchResult? matchResult = null;
        if (bestOriginalName is not null && bestDistance <= threshold)
        {
            matchResult = new MatchResult(bestOriginalName, bestDistance);
        }

        // Step 9: If match found, load full SymbolModel and return Fuzzy result
        Console.WriteLine(matchResult is not null
            ? $"Best match: [{matchResult.MatchedName}] distance={matchResult.Distance:F1}"
            : "No match found within threshold");
        if (matchResult is not null)
        {
            var symbol = await _dbContext.Symbols
                .Where(x => x.Name == matchResult.MatchedName)
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

            if (symbol is not null)
            {
                return new SearchResult
                {
                    Type = MatchType.Fuzzy,
                    Symbol = symbol,
                    Distance = matchResult.Distance,
                    SearchedText = normalizedOcrText
                };
            }
        }

        // Step 10: If no match, return NotFound with error message
        return new SearchResult
        {
            Type = MatchType.NotFound,
            Distance = 0,
            SearchedText = normalizedOcrText,
            ErrorMessage = $"No matching chip found for '{normalizedOcrText}'"
        };
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