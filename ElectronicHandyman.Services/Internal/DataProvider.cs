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
        
        // Step 3: Smart prefix filtering with multiple prefix variants
        Console.WriteLine($"Starting fuzzy search for [{normalizedOcrText}]");
        
        var candidateSet = new HashSet<string>();
        
        if (normalizedOcrText.Length >= 3)
        {
            var prefixVariants = new List<string>
            {
                normalizedOcrText[..3],
                normalizedOcrText.Length >= 4 ? normalizedOcrText[1..4] : normalizedOcrText[1..],
                normalizedOcrText.Length >= 5 ? normalizedOcrText[2..5] : normalizedOcrText[2..],
            };

            foreach (var pv in prefixVariants.Distinct())
            {
                var matches = await _dbContext.Symbols
                    .Where(x => x.Name.ToUpper().StartsWith(pv))
                    .Select(x => x.Name)
                    .ToListAsync();
                Console.WriteLine($"  Prefix [{pv}]: {matches.Count} candidates");
                foreach (var m in matches) candidateSet.Add(m);
            }
        }

        if (candidateSet.Count == 0)
        {
            Console.WriteLine("  No prefix matches, loading all candidates");
            var all = await _dbContext.Symbols.Select(x => x.Name).ToListAsync();
            foreach (var a in all) candidateSet.Add(a);
        }

        Console.WriteLine($"Total unique candidates: {candidateSet.Count}");

        // Step 7 & 8: Find best match (case-insensitive via ComputeDistance)
        string? bestOriginalName = null;
        double bestDistance = double.MaxValue;

        foreach (var candidate in candidateSet)
        {
            if (string.IsNullOrEmpty(candidate)) continue;
            var distance = _matcher.ComputeDistance(normalizedOcrText, candidate);
            if (distance < bestDistance ||
                (distance == bestDistance && bestOriginalName != null && candidate.Length < bestOriginalName.Length))
            {
                bestDistance = distance;
                bestOriginalName = candidate;
            }
        }

        MatchResult? matchResult = bestOriginalName is not null && bestDistance <= threshold
            ? new MatchResult(bestOriginalName, bestDistance)
            : null;

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