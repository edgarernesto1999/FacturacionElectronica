using Microsoft.EntityFrameworkCore;
using FacturacionElectronica.Api.Domain;

namespace FacturacionElectronica.Api.Data
{

  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Lote> Lotes => Set<Lote>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
      base.OnModelCreating(mb);

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

      // producto
      mb.Entity<Producto>(e =>
      {
        e.ToTable("Productos");
        e.HasKey(x => x.ProductoId);

        e.Property(x => x.TipoProducto).HasMaxLength(50).IsRequired();
        e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        e.Property(x => x.Activo).HasDefaultValue(true);

        e.HasIndex(x=> x.TipoProducto);
      });

      mb.Entity<Lote>(e =>
      {
        e.ToTable("Lotes");
        e.HasKey(x => x.LoteId);

        e.Property(x=>x.CostoUnitario).HasColumnType("decimal(18,2)");
        e.Property(x=>x.PrecioVentaUnitario).HasColumnType("decimal(18,2)");
        e.Property(x => x.FechaCompra).IsRequired();
        e.Property(x=>x.CantidadDisponible).IsRequired();
        e.HasCheckConstraint("CK_Lotes_Cantidades_Pos",
                    "[CantidadComprada] >= 0 AND [CantidadDisponible] >= 0");
        e.HasCheckConstraint("CK_Lotes_Precios_Pos",
            "[CostoUnitario] >= 0 AND [PrecioVentaUnitario] >= 0");
        e.HasCheckConstraint("CK_Lotes_Fechas_OK",
            "[FechaExpiracion] IS NULL OR [FechaExpiracion] >= [FechaCompra]");
        e.HasIndex(x => new { x.ProductoId, x.FechaCompra, x.LoteId }); // para FIFO / consultas
        e.HasIndex(x => new { x.ProductoId, x.CantidadDisponible });
        e.HasOne(x => x.Producto).WithMany(p => p.Lotes).HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Cascade);

      });
    }


  }


}
