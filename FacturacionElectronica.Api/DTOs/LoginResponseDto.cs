namespace FacturacionElectronica.Api.DTOs
{
  public class LoginResponseDto
  {
    public string token { get; set; } = default!;
    public DateTime expira { get; set; }
    public string nombreCompleto { get; set; } = default!;
    public string rol { get; set; } = default!;
  }
}
