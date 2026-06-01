namespace Services.Abstractions;

public interface IFileDataLoader
{
    Task LoadKicadSymFilesAsync(CancellationToken ct = default);
}