
using FirmaXadesNetCore;
using FirmaXadesNetCore.Crypto;
using FirmaXadesNetCore.Signature.Parameters;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace FacturacionElectronica.Api.Services.Sri
{
  public class FirmaElectronicaService
  {
    private readonly string _rutaCertificado;
    private readonly string _passwordCertificado;

    public FirmaElectronicaService(IWebHostEnvironment env, IConfiguration config)
    {
      _rutaCertificado = Path.Combine(env.ContentRootPath, "certificados", "firma.p12");
      _passwordCertificado = config["FirmaElectronica:Password"] ?? throw new Exception("Debe configurar 'FirmaElectronica:Password' en appsettings.json");
    }

    public byte[] FirmarXml(byte[] xmlSinFirmar)
    {
      var cert = new X509Certificate2(
          _rutaCertificado,
          _passwordCertificado,
          X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

      var parametros = new SignatureParameters
      {
        SignaturePackaging = SignaturePackaging.ENVELOPED,
        SigningDate = DateTime.Now,
        Signer = new Signer(cert)
      };

      var xadesService = new XadesService();

      using var input = new MemoryStream(xmlSinFirmar);
      var xadesDoc = xadesService.Sign(input, parametros);

      using var salida = new MemoryStream();
      xadesDoc.Save(salida);
      return salida.ToArray();
    }
  }
}
