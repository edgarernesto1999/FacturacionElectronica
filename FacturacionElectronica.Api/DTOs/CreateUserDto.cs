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
  public class UserUpdateDto
  {
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Correo { get; set; } = "";
    // Si deseas cambiar la contraseña, envía ContrasenaNueva; si es null/empty no se cambia
    public string? ContrasenaNueva { get; set; }
    public string Rol { get; set; } = "";
    public string Estado { get; set; } = "activo";
  }

}
