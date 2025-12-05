namespace FacturacionElectronica.Api.Domain
{
  public class Cliente
  {
    public string Cedula { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
  }
}
