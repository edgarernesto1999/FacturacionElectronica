using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FacturacionElectronica.Api.Services.Sri
{
  public class SriRecepcionClient
  {
    private readonly HttpClient _http;

    // Endpoint REAL del servicio (sin ?wsdl)
    private const string Endpoint =
      "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";

    static SriRecepcionClient()
    {
      // Aseguramos TLS 1.2 (buena práctica para .NET Framework; en .NET Core/5+ revisar HttpClientHandler)
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public SriRecepcionClient(HttpClient http)
    {
      _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>
    /// Envía al SRI el XML firmado. xmlFirmado puede ser:
    ///  - bytes del XML firmado (se convierten a base64 dentro del SOAP), o
    ///  - el XML en bytes y construir el sobre SOAP con el xml "crudo" (no base64) — según contrato/WSDL.
    /// </summary>
    public async Task<string> EnviarComprobanteAsync(byte[] xmlFirmado)
    {
      if (xmlFirmado == null || xmlFirmado.Length == 0)
        throw new ArgumentException("xmlFirmado no puede ser null/empty.", nameof(xmlFirmado));

      // Opción A (tu enfoque actual): enviar base64 del XML dentro del elemento <xml>
      var xmlBase64 = Convert.ToBase64String(xmlFirmado);

      // No es necesario escapar el base64 (SecurityElement.Escape), pero si decides enviar XML "crudo" -> manejar distinto.
      var soapEnvelope = $@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:validarComprobante>
         <xml>{xmlBase64}</xml>
      </ec:validarComprobante>
   </soapenv:Body>
</soapenv:Envelope>";

      using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
      {
        // Content-Type con charset explícito
        Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
      };

      // Evitar añadir SOAPAction vacío. Si tu WSDL indica una acción, añadirla aquí.
      // request.Headers.Add("SOAPAction", ""); // <-- no añadir si es vacío.

      request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

      HttpResponseMessage response = null;
      string responseBody = null;

      try
      {
        response = await _http.SendAsync(request).ConfigureAwait(false);

        responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Lanza excepción con status si no es success.
        response.EnsureSuccessStatusCode();

        return responseBody;
      }
      catch (HttpRequestException ex)
      {
        // Lanzamos excepcion más rica en info para debugging
        var status = response?.StatusCode.ToString() ?? "NoResponse";
        var statusCode = response != null ? ((int)response.StatusCode).ToString() : "0";

        var msg = new StringBuilder();
        msg.AppendLine($"Error HTTP al llamar al SRI. Status: {statusCode} - {status}.");
        msg.AppendLine($"Mensaje: {ex.Message}");
        if (!string.IsNullOrEmpty(responseBody))
        {
          msg.AppendLine("Respuesta del servicio:");
          msg.AppendLine(responseBody);
        }

        throw new HttpRequestException(msg.ToString(), ex);
      }
      catch (Exception)
      {
        // Re-lanzar para que el caller maneje según su política
        throw;
      }
      finally
      {
        // Asegurar dispose de response
        response?.Dispose();
      }
    }
  }
}
