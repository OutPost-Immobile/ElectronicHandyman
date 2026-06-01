using ElectronicHandyman.Domain.Domain;
using ElectronicHandyman.Domain.Domain.Config;
using ElectronicHandyman.Domain.Domain.Kicad;
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
    public DbSet<SymbolEntity> Symbols => Set<SymbolEntity>();
    public DbSet<CircleEntity> Circles => Set<CircleEntity>();
    public DbSet<PinEntity> Pins => Set<PinEntity>();
    public DbSet<PolylineEntity> Polylines => Set<PolylineEntity>();
    public DbSet<PolylinePointEntity> PolylinePoints => Set<PolylinePointEntity>();
    public DbSet<RectangleEntity> Rectangles => Set<RectangleEntity>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(HandymanDbContext).Assembly);
        
        base.OnModelCreating(builder);
        
        builder.Entity<SymbolEntity>()
            .HasIndex(symbol => symbol.Name)
            .IsUnique();
    }
}