using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FacturacionElectronica.Client.DTOs // O el namespace que corresponda en tu proyecto
{
  /// <summary>
  /// DTO principal para representar un cliente en listas y tablas.
  /// Corresponde a la respuesta de GET /api/clientes.
  /// </summary>
  public class ClienteDto
  {
    [JsonPropertyName("cedula")]
    public string Cedula { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("correo")]
    public string? Correo { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }
  }

  /// <summary>
  /// DTO para ver los detalles completos de un cliente.
  /// Corresponde a la respuesta de GET /api/clientes/{cedula}.
  /// </summary>
  public class ClienteDetailDto
  {
    [JsonPropertyName("cedula")]
    public string Cedula { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("correo")]
    public string? Correo { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }
  }

  /// <summary>
  /// DTO para enviar los datos para crear un nuevo cliente.
  /// Se usa en el cuerpo de la petición POST /api/clientes.
  /// </summary>
  public class ClienteCreateDto
  {
    [JsonPropertyName("cedula")]
    [Required(ErrorMessage = "La cédula es obligatoria.")]
    public string Cedula { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("correo")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string? Correo { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }
  }

  /// <summary>
  /// DTO para enviar los datos para actualizar un cliente.
  /// Se usa en el cuerpo de la petición PUT /api/clientes/{cedula}.
  /// </summary>
  public class ClienteUpdateDto
  {
    [JsonPropertyName("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("correo")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string? Correo { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }
  }

  /// <summary>
  /// Clase genérica para manejar la respuesta de la API que tiene el formato { total: ..., data: ... }
  /// </summary>
  public class ApiResponse<T>
  {
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }
  }
}
