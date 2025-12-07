using Microsoft.EntityFrameworkCore;

namespace Carbot;

public class CarbotDbContext : DbContext
{
    public CarbotDbContext(DbContextOptions<CarbotDbContext> options)
        : base(options)
    {
    }

    public DbSet<Prompt> Prompts => Set<Prompt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Text)
                  .IsRequired()
                  .HasMaxLength(2000);
        });
    }
}
