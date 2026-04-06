using System.Text.Json.Serialization;

namespace ElectronicHandyman.Scrapper.Models.Api;

public record ArduinoPageDataResponseModel
{
    [JsonPropertyName("result")]
    public required ResultModel Result { get; init; }
}

public record ResultModel
{
    [JsonPropertyName("data")]
    public required DataModel Data { get; init; }
}

public record DataModel
{
    [JsonIgnore]
    public string? BoardName { get; set; }
    
    [JsonPropertyName("downloads")] 
    public required DownloadsModel Downloads { get; init; }
    
    [JsonPropertyName("techspecs")] 
    public required TechSpecsModel TechSpecs { get; init; }
}

public record TechSpecsModel
{
    [JsonPropertyName("fields")]
    public required FieldsModel Fields { get; init; }
}

public record FieldsModel
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public record DownloadsModel
{
    [JsonPropertyName("edges")]
    public required List<EdgeModel> Edges { get; init; }
}

public record EdgeModel
{
    [JsonPropertyName("node")]
    public required NodeModel NodeModel { get; init; }
}

public record NodeModel
{
    [JsonPropertyName("base")]
    public required string Base { get; init; }

    [JsonPropertyName("publicURL")]
    public required string PublicUrl { get; init; }
}