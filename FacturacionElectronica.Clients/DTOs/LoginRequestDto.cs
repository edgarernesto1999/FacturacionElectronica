using System.ComponentModel.DataAnnotations;

namespace FacturacionElectronica.Clients.DTOs
{
  public class LoginRequestDto
  {
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; }
  }
}
