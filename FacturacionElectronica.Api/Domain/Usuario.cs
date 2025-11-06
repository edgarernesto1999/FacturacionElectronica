namespace FacturacionElectronica.Api.Domain
{
  public class Usuario
  {
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Apellido { get; set; } = default!;
    public string ContrasenaHash { get; set; } = default!;
    public string Rol { get; set; } = default!;    // "admin" | "empleado"
    public string Correo { get; set; } = default!;
    public string Estado { get; set; } = "activo"; // "activo" | "inactivo"
  }
}
