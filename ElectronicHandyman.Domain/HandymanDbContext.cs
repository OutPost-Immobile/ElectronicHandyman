using ElectronicHandyman.Domain.Domain;
using ElectronicHandyman.Domain.Domain.Config;
using Microsoft.EntityFrameworkCore;

namespace ElectronicHandyman.Domain;

public class HandymanDbContext : DbContext
{
    public HandymanDbContext()
    {
    }

    public HandymanDbContext(DbContextOptions<HandymanDbContext> options) : base(options)
    {
    }
    
    public DbSet<BoardEntity> Boards => Set<BoardEntity>();
    public DbSet<BoardFamilyEntity> BoardFamilies => Set<BoardFamilyEntity>();
    public DbSet<BoardDocumentEntity> BoardDocuments => Set<BoardDocumentEntity>();
    public DbSet<TexasInstrumentsApiConfigEntity> TexasApiConfig => Set<TexasInstrumentsApiConfigEntity>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(HandymanDbContext).Assembly);
        
        base.OnModelCreating(builder);
    }
}