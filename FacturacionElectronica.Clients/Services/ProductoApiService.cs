using FacturacionElectronica.Clients.DTOs;
using FacturacionElectronica.Clients.Utils; // Necesario para el convertidor
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FacturacionElectronica.Clients.Services
{
  public class ProductoApiService
  {
    private readonly HttpClient _httpClient;

    public ProductoApiService(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    /// <summary>
    /// Obtiene la lista de todos los productos desde la API.
    /// </summary>
    public async Task<List<ProductListItemDto>> GetProductosAsync()
    {
      // --- CAMBIO CLAVE AQUÍ ---
      // Le indicamos al deserializador que la propiedad "data" en la respuesta
      // de la API contiene una LISTA de productos (List<ProductListItemDto>),
      // no solo un objeto individual.
      var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ProductListItemDto>>>("api/productos");

      // Esta línea ahora funcionará correctamente porque "response.Data" contendrá la lista de productos.
      return response?.Data ?? new List<ProductListItemDto>();
    }

    /// <summary>
    /// Envía una solicitud para crear un nuevo producto.
    /// </summary>
    public async Task<HttpResponseMessage> AddProductoAsync(ProductCreateDto productToCreate)
    {
      string jsonContent = JsonSerializer.Serialize(productToCreate);
      var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
      return await _httpClient.PostAsync("api/productos", httpContent);
    }

    /// <summary>
    /// Envía una solicitud para crear un nuevo lote para un producto existente.
    /// </summary>
    public async Task<HttpResponseMessage> AddLoteAsync(LoteCreateDto loteToCreate)
    {
      // Opciones para enseñarle al serializador a manejar DateOnly
      var serializerOptions = new JsonSerializerOptions
      {
        Converters = { new DateOnlyJsonConverter() }
      };

      // Serializar usando las opciones
      string jsonContent = JsonSerializer.Serialize(loteToCreate, serializerOptions);

      var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
      return await _httpClient.PostAsync("api/lotes", httpContent);
    }
    /// <summary>
    /// Obtiene la lista de todos los productos, incluyendo sus lotes anidados, desde la API.
    /// Este es el método que tu nueva vista de productos y lotes debe usar.
    /// </summary>
    public async Task<List<ProductWithLotesDto>> GetProductosConLotesAsync()
    {
      // Le indicamos al deserializador que la respuesta contiene una lista del DTO
      // que incluye la propiedad "Lotes".
      var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ProductWithLotesDto>>>("api/productos");

      // Devuelve la lista de productos con sus lotes, o una lista vacía si no hay datos.
      return response?.Data ?? new List<ProductWithLotesDto>();
    }
  }
}
