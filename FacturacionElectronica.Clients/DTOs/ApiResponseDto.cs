// Ubicación: FacturacionElectronica.Clients/DTOs/ApiResponseDto.cs
using System.Text.Json.Serialization;

/// <summary>
/// Representa la estructura estándar de respuesta de la API.
/// Es una clase genérica que puede contener una lista de cualquier tipo de DTO.
/// </summary>
/// <typeparam name="T">El tipo de dato que contendrá la lista 'Data' (ej: UserDto, ProductDto)</typeparam>
public class ApiResponseDto<T>
{
  /// <summary>
  /// El número total de registros disponibles. Mapea desde la propiedad "total" del JSON.
  /// </summary>
  [JsonPropertyName("total")]
  public int Total { get; set; }

  /// <summary>
  /// La lista de datos solicitados. Mapea desde la propiedad "data" del JSON.
  /// </summary>
  [JsonPropertyName("data")]
  public List<T> Data { get; set; } = new List<T>();
}
