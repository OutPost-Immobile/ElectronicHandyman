using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicHandyman.Domain.Domain.Config;

public class TexasInstrumentsApiConfigEntity
{
    public Guid Id { get; }
    public string AccessToken { get; set; }
}

internal class TexasInstrumentsApiConfigEntityConfiguration :  IEntityTypeConfiguration<TexasInstrumentsApiConfigEntity>
{
    public void Configure(EntityTypeBuilder<TexasInstrumentsApiConfigEntity> builder)
    {
        builder.HasKey(x => x.Id);
    }
}