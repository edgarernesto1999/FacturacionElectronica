public class EnvioFacturaDto
{
  public string EmailDestino { get; set; }
  public string Asunto { get; set; } = "Factura electrónica";
  public string Mensaje { get; set; } = "Estimado cliente, adjuntamos su factura electrónica.";
  public string AdjuntoPdfBase64 { get; set; }
  public string AdjuntoXmlBase64 { get; set; }
}
