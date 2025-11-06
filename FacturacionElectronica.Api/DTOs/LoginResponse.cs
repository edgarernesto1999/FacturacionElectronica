namespace FacturacionElectronica.Api.DTOs
{
  public record LoginResponse(string Token, DateTime Expira, string NombreCompleto, string Rol)
  {
  }
}
