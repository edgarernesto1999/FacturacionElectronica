// Asegúrate de que el namespace sea el correcto para tu proyecto
namespace FacturacionElectronica.Clients.DTOs;

// DTO para CREAR o ACTUALIZAR un producto.
public record ProductUpdateDto(
    string TipoProducto,
    string Nombre,
    bool Activo
);

// DTO que representa un Lote. Se define primero.
public class LoteApiResponseDto
{
  public int LoteId { get; set; }
  public DateTime FechaCompra { get; set; }
  public DateTime? FechaExpiracion { get; set; }
  public decimal CostoUnitario { get; set; }
  public decimal PrecioVentaUnitario { get; set; }
  public int CantidadComprada { get; set; }
  public int CantidadDisponible { get; set; }
}

// DTO para un item en la lista de productos.
public class ProductListItemDto
{
  public int ProductId { get; set; }
  public string Nombre { get; set; } = string.Empty;
  public string TipoProducto { get; set; } = string.Empty;
  public bool Activo { get; set; }
  // Esta es la propiedad que el compilador no encontraba
  public List<LoteApiResponseDto> Lotes { get; set; } = new();
}

// DTO para la respuesta de un único producto.
public class ProductoApiResponseDto
{
  public int ProductoId { get; set; }
  public string TipoProducto { get; set; } = string.Empty;
  public string Nombre { get; set; } = string.Empty;
  public bool Activo { get; set; }
  public List<LoteApiResponseDto> Lotes { get; set; } = new();
}

// DTO para la respuesta completa de la lista de productos.
// Esta es la clase que el compilador no encontraba.
public class ProductListApiResponse
{
  public int Total { get; set; }
  public List<ProductListItemDto> Data { get; set; } = new();
}
