namespace FacturacionElectronica.Api.DTOs
{
  public class CreateUserDto
  {
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    // "admin" | "empleado"
    public string Rol { get; set; } = "empleado";
    // "activo" | "inactivo" (opcional)
    public string? Estado { get; set; } = "activo";
  }
}
