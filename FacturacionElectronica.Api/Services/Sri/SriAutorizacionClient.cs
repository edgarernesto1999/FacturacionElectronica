using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

// Asegúrate de que el namespace sea el correcto para tu proyecto
namespace FacturacionElectronica.Api.Services
{
  public class SriAutorizacionClient
  {
    // ... todo el código de tu clase SriAutorizacionClient va aquí ...
    // ... (el constructor, el método AutorizarAsync, etc.)
    // 
    private readonly HttpClient _http;
    private const string UrlAutorizacion = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";

    public SriAutorizacionClient(HttpClient http)
    {
      _http = http;
    }

    public async Task<SriAutorizacionResult> AutorizarAsync(string claveAcceso)
    {
      var soap = $@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:autorizacionComprobante>
         <claveAccesoComprobante>{System.Security.SecurityElement.Escape(claveAcceso)}</claveAccesoComprobante>
      </ec:autorizacionComprobante>
   </soapenv:Body>
</soapenv:Envelope>";

      var req = new HttpRequestMessage(HttpMethod.Post, UrlAutorizacion)
      {
        Content = new StringContent(soap, Encoding.UTF8, "text/xml")
      };
      req.Headers.Add("SOAPAction", "");

      var resp = await _http.SendAsync(req);
      var responseString = await resp.Content.ReadAsStringAsync();

      // <<< MEJORA: Añadimos un log para ver SIEMPRE la respuesta cruda del SRI
      System.Console.WriteLine("===== RESPUESTA CRUDA DEL SRI =====");
      System.Console.WriteLine(responseString);
      System.Console.WriteLine("===================================");

      return ParseAutorizacionResponse(responseString);
    }

    private SriAutorizacionResult ParseAutorizacionResponse(string responseXml)
    {
      var result = new SriAutorizacionResult { RawXml = responseXml };
      var doc = new XmlDocument();

      // ==================================================================
      // ▼▼▼ CORRECCIÓN PRINCIPAL: Manejo de respuestas no-XML ▼▼▼
      // ==================================================================
      try
      {
        doc.LoadXml(responseXml); // Esto fallará si la respuesta es HTML
      }
      catch (XmlException ex)
      {
        // La respuesta no es un XML válido. Probablemente es una página de error HTML.
        result.Estado = "ERROR_COMUNICACION";
        result.Mensajes = $"La respuesta del SRI no es un XML válido. Posiblemente el servicio está caído. Error: {ex.Message}";
        // Devolvemos el resultado con el error y el HTML crudo para análisis.
        return result;
      }
      // ==================================================================
      // ▲▲▲ FIN DE LA CORRECCIÓN ▲▲▲
      // ==================================================================

      var nsmgr = new XmlNamespaceManager(doc.NameTable);
      nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
      nsmgr.AddNamespace("sri", "http://ec.gob.sri.ws.autorizacion");

      var autorizacionNode = doc.SelectSingleNode("//sri:autorizacion", nsmgr) ?? doc.SelectSingleNode("//autorizacion", nsmgr);

      if (autorizacionNode != null)
      {
        result.Estado = autorizacionNode["estado"]?.InnerText;
        result.NumeroAutorizacion = autorizacionNode["numeroAutorizacion"]?.InnerText;
        result.FechaAutorizacion = autorizacionNode["fechaAutorizacion"]?.InnerText;
        result.XmlAutorizado = autorizacionNode["comprobante"]?.InnerText;

        var mensajesNode = autorizacionNode.SelectSingleNode("mensajes");
        if (mensajesNode != null)
        {
          var sb = new StringBuilder();
          foreach (XmlNode msgNode in mensajesNode.SelectNodes("mensaje"))
          {
            sb.AppendLine($"[{msgNode["identificador"]?.InnerText}] {msgNode["mensaje"]?.InnerText} ({msgNode["informacionAdicional"]?.InnerText})".Trim());
          }
          result.Mensajes = sb.ToString();
        }
      }
      else
      {
        // Manejo de otros casos donde la respuesta es XML pero no tiene el nodo esperado
        result.Estado = doc.SelectSingleNode("//estado", nsmgr)?.InnerText ?? "NO_PROCESADA";
        // ... (código para extraer mensajes de error si los hubiera)
      }

      return result;
    }
  }

  // ▼▼▼ PEGA ESTA CLASE AQUÍ ▼▼▼
  // La definición de la clase que faltaba
  public class SriAutorizacionResult
  {
    public string Estado { get; set; }
    public string NumeroAutorizacion { get; set; }
    public string FechaAutorizacion { get; set; }
    public string XmlAutorizado { get; set; }
    public string Mensajes { get; set; }
    public string RawXml { get; set; }
  }
}
