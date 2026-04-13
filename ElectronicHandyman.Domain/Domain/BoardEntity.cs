using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicHandyman.Domain.Domain;

public class BoardEntity
{
    public int Id { get; }
    public required string ModelName { get; set; }
    public required string Href { get; set; }
    
    public int BoardFamilyId { get; set; }
    public BoardFamilyEntity BoardFamily { get; set; }

    public virtual ICollection<BoardDocumentEntity> Documents { get; set; } = [];
}

internal class BoardEntityConfiguration : IEntityTypeConfiguration<BoardEntity>
{
    public void Configure(EntityTypeBuilder<BoardEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.ModelName)
            .IsUnique();
        
        builder.HasOne(x => x.BoardFamily)
            .WithMany(x => x.Boards)
            .HasForeignKey(x => x.BoardFamilyId);
        
        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Board)
            .HasForeignKey(x => x.BoardId);
    }
}