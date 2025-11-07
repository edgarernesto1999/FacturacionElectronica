namespace FacturacionElectronica.Api.Domain
{
  public class Lote
  {
    public int LoteId { get; set; }
    public int ProductoId { get; set; }
    public DateOnly FechaCompra { get; set; }
    public DateOnly? FechaExpiracion { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public int CantidadComprada { get; set; }
    public int CantidadDisponible { get; set; }
    // Navigation properties
    public Producto Producto { get; set; } = null!;
  }
}
