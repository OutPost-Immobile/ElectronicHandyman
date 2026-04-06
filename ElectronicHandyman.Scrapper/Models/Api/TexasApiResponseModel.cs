using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElectronicHandyman.Scrapper.Models.Api;

public record TexasApiResponseModel
{
    [JsonPropertyName("Content")]
    public required List<Product> Content { get; init; }

    [JsonPropertyName("Last")]
    public required bool Last { get; init; }

    [JsonPropertyName("TotalPages")]
    public required int TotalPages { get; init; }

    [JsonPropertyName("TotalElements")]
    public required int TotalElements { get; init; }

    [JsonPropertyName("First")]
    public required bool First { get; init; }

    [JsonPropertyName("NumberOfElements")]
    public required int NumberOfElements { get; init; }

    [JsonPropertyName("Size")]
    public required int Size { get; init; }

    [JsonPropertyName("Number")]
    public required int Number { get; init; }
}

public record Product
{
    [JsonPropertyName("Identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("Description")]
    public required string Description { get; init; }

    [JsonPropertyName("GenericProductIdentifier")]
    public required string GenericProductIdentifier { get; init; }

    [JsonPropertyName("Url")]
    public required string Url { get; init; }

    [JsonPropertyName("ProductFamilyDescription")]
    public required string ProductFamilyDescription { get; init; }

    [JsonPropertyName("DatasheetUrl")]
    public required string DatasheetUrl { get; init; }

    [JsonPropertyName("LifeCycleStatus")]
    public required string LifeCycleStatus { get; init; }

    [JsonPropertyName("Price")]
    public required PriceDetail Price { get; init; }

    [JsonPropertyName("LeadTimeWeeks")]
    public required string LeadTimeWeeks { get; init; }

    [JsonPropertyName("InventoryStatus")]
    public required string InventoryStatus { get; init; }

    [JsonPropertyName("FullBoxQty")]
    public required int FullBoxQty { get; init; }

    [JsonPropertyName("MinOrderQty")]
    public required int MinOrderQty { get; init; }

    [JsonPropertyName("NextIncrementQty")]
    public required int? NextIncrementQty { get; init; }

    [JsonPropertyName("StandardPackQty")]
    public required int StandardPackQty { get; init; }

    [JsonPropertyName("OkayToOrder")]
    public required bool OkayToOrder { get; init; }

    [JsonPropertyName("StopShip")]
    public required bool StopShip { get; init; }

    [JsonPropertyName("Obsolete")]
    public required bool Obsolete { get; init; }

    [JsonPropertyName("LifetimeBuy")]
    public required bool LifetimeBuy { get; init; }

    [JsonPropertyName("ChangeOrderWindow")]
    public required string ChangeOrderWindow { get; init; }

    [JsonPropertyName("ExtendedShelfLife")]
    public required bool ExtendedShelfLife { get; init; }

    [JsonPropertyName("ExportControlClassificationNumber")]
    public required string ExportControlClassificationNumber { get; init; }

    [JsonPropertyName("HtsCode")]
    public required string HtsCode { get; init; }

    [JsonPropertyName("MilitaryGoods")]
    public required bool MilitaryGoods { get; init; }

    [JsonPropertyName("Pin")]
    public required int Pin { get; init; }

    [JsonPropertyName("PackageType")]
    public required string PackageType { get; init; }

    [JsonPropertyName("PackageGroup")]
    public required string PackageGroup { get; init; }

    [JsonPropertyName("IndustryPackageType")]
    public required string IndustryPackageType { get; init; }

    [JsonPropertyName("JedecCode")]
    public required string JedecCode { get; init; }

    [JsonPropertyName("PackageCarrier")]
    public required string PackageCarrier { get; init; }

    [JsonPropertyName("Width")]
    public required double Width { get; init; }

    [JsonPropertyName("Length")]
    public required double Length { get; init; }

    [JsonPropertyName("Thickness")]
    public required double Thickness { get; init; }

    [JsonPropertyName("Pitch")]
    public required double Pitch { get; init; }

    [JsonPropertyName("MaxHeight")]
    public required double MaxHeight { get; init; }

    [JsonPropertyName("QualityEstimatorUrl")]
    public required string QualityEstimatorUrl { get; init; }

    [JsonPropertyName("MaterialContentUrl")]
    public required string MaterialContentUrl { get; init; }
}

public record PriceDetail
{
    [JsonPropertyName("Value")]
    public required decimal Value { get; init; }

    [JsonPropertyName("Quantity")]
    public required int Quantity { get; init; }
}