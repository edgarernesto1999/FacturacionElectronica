using Microsoft.EntityFrameworkCore;
using FacturacionElectronica.Api.Domain;
using FacturacionElectronica.Api.Domain.Facturacion;

namespace FacturacionElectronica.Api.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
      base.OnModelCreating(mb);
      mb.HasDefaultSchema("dbo");

      // ================= USUARIOS =================
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

      // ================= PRODUCTOS =================
      mb.Entity<Producto>(e =>
      {
        e.ToTable("Productos");
        e.HasKey(x => x.ProductoId);
        e.Property(x => x.TipoProducto).HasMaxLength(50).IsRequired();
        e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        e.Property(x => x.Marca).HasMaxLength(50).IsRequired();
        e.Property(x => x.Presentacion).HasMaxLength(100).IsRequired();
        e.Property(x => x.Activo).HasDefaultValue(true);
        e.HasIndex(x => x.TipoProducto);
      });

      // ================= LOTES =================
      mb.Entity<Lote>(e =>
      {
        e.ToTable("Lotes");
        e.HasKey(x => x.LoteId);
        e.Property(x => x.CostoUnitario).HasColumnType("decimal(18,2)");
        e.Property(x => x.PrecioVentaUnitario).HasColumnType("decimal(18,2)");
        e.Property(x => x.FechaCompra).IsRequired();
        e.Property(x => x.CantidadDisponible).IsRequired();
        e.HasCheckConstraint("CK_Lotes_Cantidades_Pos", "[CantidadComprada] >= 0 AND [CantidadDisponible] >= 0");
        e.HasCheckConstraint("CK_Lotes_Precios_Pos", "[CostoUnitario] >= 0 AND [PrecioVentaUnitario] >= 0");
        e.HasCheckConstraint("CK_Lotes_Fechas_OK", "[FechaExpiracion] IS NULL OR [FechaExpiracion] >= [FechaCompra]");
        e.HasIndex(x => new { x.ProductoId, x.FechaCompra, x.LoteId });
        e.HasIndex(x => new { x.ProductoId, x.CantidadDisponible });
        e.HasOne(x => x.Producto)
         .WithMany(p => p.Lotes)
         .HasForeignKey(x => x.ProductoId)
         .OnDelete(DeleteBehavior.Cascade);
      });

      // ================= CLIENTES =================
      mb.Entity<Cliente>(e =>
      {
        e.ToTable("Clientes");
        e.HasKey(x => x.Cedula);
        e.Property(x => x.Cedula).HasMaxLength(20);
        e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
        e.Property(x => x.Direccion).HasMaxLength(255);
        e.Property(x => x.Correo).HasMaxLength(255);
        e.Property(x => x.Telefono).HasMaxLength(20);
        e.HasIndex(x => x.Correo).IsUnique().HasFilter("[Correo] IS NOT NULL");
      });

      // ================= FACTURAS =================
      mb.Entity<Factura>(e =>
      {
        e.ToTable("Facturas");
        e.HasKey(f => f.Id);

        e.Property(f => f.Numero)
          .HasMaxLength(20)
          .IsRequired();

        e.Property(f => f.ClaveAcceso)            // 👈 NUEVA PROPIEDAD
          .HasMaxLength(49)
          .IsRequired();

        e.Property(f => f.FechaEmision)
          .HasColumnType("datetime2")
          .IsRequired();

        // Emisor
        e.Property(f => f.RucEmisor).HasMaxLength(13).IsRequired();
        e.Property(f => f.RazonSocialEmisor).HasMaxLength(200).IsRequired();
        e.Property(f => f.DireccionMatriz).HasMaxLength(255).IsRequired();

        // Cliente
        e.Property(f => f.TipoIdentificacionCliente).HasMaxLength(5).IsRequired();
        e.Property(f => f.IdentificacionCliente).HasMaxLength(20).IsRequired();
        e.Property(f => f.NombreCliente).HasMaxLength(200).IsRequired();
        e.Property(f => f.DireccionCliente).HasMaxLength(255);

        // Totales
        e.Property(f => f.SubtotalSinImpuestos).HasColumnType("decimal(18,2)");
        e.Property(f => f.TotalDescuento).HasColumnType("decimal(18,2)");
        e.Property(f => f.TotalIva).HasColumnType("decimal(18,2)");
        e.Property(f => f.ImporteTotal).HasColumnType("decimal(18,2)");

        // Relación 1-N con detalles
        e.HasMany(f => f.Detalles)
         .WithOne(d => d.Factura)
         .HasForeignKey(d => d.FacturaId)
         .OnDelete(DeleteBehavior.Cascade);
      });

      // ================= DETALLES FACTURA =================
      mb.Entity<FacturaDetalle>(e =>
      {
        e.ToTable("FacturaDetalles");
        e.HasKey(d => d.FacturaDetalleId);

        e.Property(d => d.Codigo).HasMaxLength(50).IsRequired();
        e.Property(d => d.Descripcion).HasMaxLength(200).IsRequired();

        e.Property(d => d.Cantidad).HasColumnType("decimal(18,6)");
        e.Property(d => d.PrecioUnitario).HasColumnType("decimal(18,6)");
        e.Property(d => d.Descuento).HasColumnType("decimal(18,2)");
        e.Property(d => d.TotalSinImpuesto).HasColumnType("decimal(18,2)");
        e.Property(d => d.Iva).HasColumnType("decimal(18,2)");
      });
    }
  }
}
