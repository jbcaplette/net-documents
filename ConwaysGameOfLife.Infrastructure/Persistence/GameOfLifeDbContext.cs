using ConwaysGameOfLife.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ConwaysGameOfLife.Infrastructure.Persistence;

public class GameOfLifeDbContext : DbContext
{
    public GameOfLifeDbContext(DbContextOptions<GameOfLifeDbContext> options) : base(options)
    {
        // Optimize for performance
        ChangeTracker.AutoDetectChangesEnabled = false;
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<BoardEntity> Boards { get; set; } = null!;
    public DbSet<BoardHistoryEntity> BoardHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BoardEntity
        modelBuilder.Entity<BoardEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasConversion(
                    boardId => boardId.Value,
                    guid => new BoardId(guid));

            entity.Property(e => e.AliveCellsJson)
                .HasColumnName("AliveCells")
                .IsRequired();

            entity.Property(e => e.Generation).IsRequired();
            entity.Property(e => e.MaxDimension).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastUpdatedAt).IsRequired();

            entity.HasIndex(e => e.Id).IsUnique();
        });

        // Configure BoardHistoryEntity
        modelBuilder.Entity<BoardHistoryEntity>(entity =>
        {
            entity.HasKey(e => new { e.BoardId, e.Generation });
            
            entity.Property(e => e.BoardId)
                .HasConversion(
                    boardId => boardId.Value,
                    guid => new BoardId(guid));

            entity.Property(e => e.AliveCellsJson)
                .HasColumnName("AliveCells")
                .IsRequired();

            entity.Property(e => e.StateHash).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Performance-oriented indexes
            entity.HasIndex(e => e.BoardId);
            entity.HasIndex(e => e.StateHash);
            entity.HasIndex(e => new { e.BoardId, e.StateHash }); // Composite index for cycle detection
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Manually detect changes for better performance control
        ChangeTracker.DetectChanges();
        return base.SaveChangesAsync(cancellationToken);
    }
}