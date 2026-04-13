using ElectronicHandyman.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicHandyman.Domain.Domain;

public class BoardDocumentEntity
{
    public int Id { get; }
    public required DocumentType DocumentType { get; set; }
    public required string FileName { get; set; }
    public required string StaticUrl { get; set; }

    public int BoardId { get; set; }
    public BoardEntity Board { get; set; }
}

internal class BoardDocumentEntityConfiguration : IEntityTypeConfiguration<BoardDocumentEntity>
{
    public void Configure(EntityTypeBuilder<BoardDocumentEntity> builder)
    {
        builder.HasKey(x => x.Id);
    }
}