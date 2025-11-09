using System.Text.Json.Serialization;

namespace FacturacionElectronica.Clients.DTOs
{
  // DTO para crear un nuevo producto
  public record ProductCreateDto(
      string TipoProducto,
      string Nombre,
      bool Activo = true
  );

  // DTO para actualizar un producto existente
  public record ProductUpdateDto(
      string TipoProducto,
      string Nombre,
      bool Activo
  );

  // --- CORRECCIÓN PRINCIPAL AQUÍ ---
  // DTO para mostrar los productos en una lista, sincronizado con tu API.
  public class ProductListItemDto
  {
    // La corrección clave: el nombre en el JSON es "productoId".
    [JsonPropertyName("productoId")]
    public int ProductId { get; set; }

    [JsonPropertyName("tipoProducto")]
    public string TipoProducto { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    // NOTA: Las propiedades como "lotes" o "stock" del JSON se ignorarán
    // durante la deserialización porque no están definidas aquí,
    // lo cual es perfecto para tu caso de uso.
  }

  // Clase genérica para manejar la respuesta de la API que tiene el formato { total: ..., data: ... }
  public class ApiResponse<T>
  {
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }
  }
}
