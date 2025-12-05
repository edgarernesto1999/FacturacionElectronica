using System;
using System.Linq;
using System.Text;

namespace FacturacionElectronica.Api.Services.Sri
{
  public static class ClaveAccesoService
  {
    public static string GenerarClaveAcceso(DateTime fecha, string ruc,
        string tipoComprobante, string establecimiento, string puntoEmision,
        string secuencial, string tipoEmision = "1")
    {
      string fechaStr = fecha.ToString("ddMMyyyy");

      string ambiente = "1"; // PRUEBAS
      string codigoNumerico = new Random().Next(10000000, 99999999).ToString(); // 8 dígitos

      string baseClave = $"{fechaStr}{tipoComprobante}{ruc}{ambiente}{establecimiento}{puntoEmision}{secuencial}{codigoNumerico}{tipoEmision}";

      string dv = DigitoVerificador(baseClave);

      return baseClave + dv;
    }

    private static string DigitoVerificador(string cadena)
    {
      int[] coef = { 4, 3, 2, 7, 6, 5, 4, 3, 2 };
      int suma = 0;
      int k = 0;

      for (int i = cadena.Length - 1; i >= 0; i--)
      {
        suma += (cadena[i] - '0') * coef[k];
        k = (k + 1) % coef.Length;
      }

      int mod = 11 - (suma % 11);
      if (mod == 11) mod = 0;
      if (mod == 10) mod = 1;

      return mod.ToString();
    }
  }
}
