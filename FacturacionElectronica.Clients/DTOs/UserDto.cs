// Ubicación: FacturacionElectronica.Clients/DTOs/UserDto.cs
public class UserDto
{
  public int Id { get; set; } // <-- ASEGÚRATE DE QUE ESTA LÍNEA EXISTA
  public string Nombre { get; set; } = string.Empty;
  public string Apellido { get; set; } = string.Empty;
  public string Correo { get; set; } = string.Empty;
  public string Rol { get; set; } = string.Empty;
  public string Estado { get; set; } = string.Empty;
}
