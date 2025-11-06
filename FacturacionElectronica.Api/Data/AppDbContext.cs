using Microsoft.EntityFrameworkCore;
using FacturacionElectronica.Api.Domain;

namespace FacturacionElectronica.Api.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
      mb.HasDefaultSchema("dbo");

      mb.Entity<Usuario>(e =>
      {
        e.ToTable("Usuarios");
        e.HasKey(x => x.Id);
        e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
        e.Property(x => x.ContrasenaHash).HasMaxLength(255).IsRequired();
        e.Property(x => x.Rol).HasMaxLength(20).IsRequired();
        e.Property(x => x.Correo).HasMaxLength(255).IsRequired();
        e.Property(x => x.Estado).HasMaxLength(20).HasDefaultValue("activo").IsRequired();
        e.HasIndex(x => x.Correo).IsUnique();
      });
    }
  }
}
