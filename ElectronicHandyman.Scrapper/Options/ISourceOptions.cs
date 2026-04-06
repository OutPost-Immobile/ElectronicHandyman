using System.ComponentModel.DataAnnotations;

namespace ElectronicHandyman.Scrapper.Options;

internal interface ISourceOptions
{
    static abstract string SectionName { get; }
}

internal abstract record SourceOptionsBase
{
    [Required]
    public required string Url { get; init; }
}

internal record ArduinoOptions : SourceOptionsBase, ISourceOptions
{
    public static string SectionName => "Arduino";
}

internal record PlatformIOOPtions : SourceOptionsBase, ISourceOptions
{
    public static string SectionName => "PlatformIO";
}

internal record TexasOptions : SourceOptionsBase, ISourceOptions
{
    public static string SectionName => "TexasInstruments";
    
    [Required]
    public required string Key { get; init; }
    
    [Required]
    public required string Secret { get; init; }
}