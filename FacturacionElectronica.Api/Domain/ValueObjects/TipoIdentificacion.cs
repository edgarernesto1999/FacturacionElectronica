namespace FacturacionElectronica.Api.Domain.ValueObjects
{
  public enum TipoIdentificacion
  {
    Cedula,
    Ruc,
    Pasaporte,
    Desconocida
  }
  public static class IdentificacionHelper
  {
    public static TipoIdentificacion DetectarTipo(string id)
    {
      id = (id ?? "").Trim();

      if (string.IsNullOrEmpty(id))
        return TipoIdentificacion.Desconocida;

      var soloNumeros = id.All(char.IsDigit);

      if (soloNumeros && id.Length == 10)
        return TipoIdentificacion.Cedula;

      if (soloNumeros && id.Length == 13)
        return TipoIdentificacion.Ruc;

      // Si tiene letras o longitud diferente, lo tratamos como pasaporte
      if (id.Length >= 6 && id.Length <= 20)
        return TipoIdentificacion.Pasaporte;

      return TipoIdentificacion.Desconocida;
    }



    public static bool EsCedulaValida(string cedula)
    {
      if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
        return false;

      var provincia = int.Parse(cedula.Substring(0, 2));
      if (provincia < 1 || provincia > 24) // provincias válidas 01–24
        return false;

      var tercerDigito = int.Parse(cedula[2].ToString());
      if (tercerDigito < 0 || tercerDigito > 5) // personas naturales
        return false;

      int suma = 0;
      for (int i = 0; i < 9; i++)
      {
        int digito = int.Parse(cedula[i].ToString());
        if (i % 2 == 0) // posiciones pares 0,2,4... (coeficiente 2)
        {
          digito *= 2;
          if (digito > 9) digito -= 9;
        }
        suma += digito;
      }

      int decenaSuperior = ((suma + 9) / 10) * 10;
      int digitoVerificadorCalculado = decenaSuperior - suma;
      if (digitoVerificadorCalculado == 10)
        digitoVerificadorCalculado = 0;

      int digitoVerificadorReal = int.Parse(cedula[9].ToString());
      return digitoVerificadorCalculado == digitoVerificadorReal;
    }





    public static bool EsRucValido(string ruc)
    {
      if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 13 || !ruc.All(char.IsDigit))
        return false;

      // Provincia
      var provincia = int.Parse(ruc.Substring(0, 2));
      if (provincia < 1 || provincia > 24)
        return false;

      // Tercer dígito puede ser:
      // 0-5 persona natural, 6 público, 9 privado
      var tercer = int.Parse(ruc[2].ToString());
      if (!(tercer >= 0 && tercer <= 5 || tercer == 6 || tercer == 9))
        return false;

      // Los últimos 3 dígitos no pueden ser "000"
      if (ruc.Substring(10, 3) == "000")
        return false;

      // Para simplificar, podrías no implementar el cálculo completo de dígito verificador.
      // Si quieres ser más estricto, se puede, pero para la demo de la U esto ya es aceptable.
      return true;
    }


    public static bool EsPasaporteValido(string pasaporte)
    {
      if (string.IsNullOrWhiteSpace(pasaporte))
        return false;

      pasaporte = pasaporte.Trim();

      // Reglas simples: longitud 6–20, letras y números
      if (pasaporte.Length < 6 || pasaporte.Length > 20)
        return false;

      return pasaporte.All(ch => char.IsLetterOrDigit(ch));
    }

  }
}
