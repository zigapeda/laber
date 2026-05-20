using Laber.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Laber.Shared.Data;

public sealed class LaberDbContext : DbContext
{
    public LaberDbContext(DbContextOptions<LaberDbContext> options)
        : base(options)
    {
    }

    public DbSet<IrcMessage> Messages => Set<IrcMessage>();

    public DbSet<IrcConnectionState> ConnectionState => Set<IrcConnectionState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IrcMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.Channel, m.Id });
            entity.Property(m => m.Channel).HasMaxLength(256);
            entity.Property(m => m.Sender).HasMaxLength(128);
            entity.Property(m => m.Text).HasMaxLength(4096);
        });

        modelBuilder.Entity<IrcConnectionState>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Server).HasMaxLength(256);
            entity.Property(s => s.Nick).HasMaxLength(64);
            entity.Property(s => s.LastError).HasMaxLength(1024);
        });
    }
}
