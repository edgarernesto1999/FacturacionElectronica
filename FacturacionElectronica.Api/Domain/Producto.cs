namespace FacturacionElectronica.Api.Domain
{
  public class Producto
  {
    public int ProductoId { get; set; }
    public string TipoProducto { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public String Marca { get; set; } = null!;
    public String Presentacion { get; set; } = null!;
    public bool Activo { get; set; } = true;

    // Navigation properties
    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
  }
}
