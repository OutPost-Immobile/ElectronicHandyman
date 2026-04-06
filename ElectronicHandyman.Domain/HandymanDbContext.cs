using ElectronicHandyman.Domain.Domain;
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
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(HandymanDbContext).Assembly);
        
        base.OnModelCreating(builder);
    }
}