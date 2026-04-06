using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicHandyman.Domain.Domain;

public class BoardFamilyEntity
{
    public int Id { get; }
    public required string FamilyName { get; init; }
    public required ICollection<BoardEntity> Boards { get; set; }
}

internal class BoardFamilyEntityConfiguration : IEntityTypeConfiguration<BoardFamilyEntity>
{
    public void Configure(EntityTypeBuilder<BoardFamilyEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder
            .HasIndex(x => x.FamilyName)
            .IsUnique();
        
        builder.HasMany(x => x.Boards)
            .WithOne(x => x.BoardFamily)
            .HasForeignKey(x => x.BoardFamilyId);
    }
}