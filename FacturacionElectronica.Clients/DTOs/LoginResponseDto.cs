// Ubicación: FacturacionElectronica.Clients/DTOs/LoginResponseDto.cs
using System.Text.Json.Serialization;

namespace FacturacionElectronica.Clients.DTOs
{
  public class LoginResponseDto
  {
    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; }

    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; }
  }
}
