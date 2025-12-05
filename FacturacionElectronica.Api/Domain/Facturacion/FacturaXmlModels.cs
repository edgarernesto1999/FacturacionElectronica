using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FacturacionElectronica.Api.Domain.Facturacion
{
  [XmlRoot("factura")]
  // using ...

  public class FacturaXml
  {
    [XmlAttribute("id")]
    public string Id { get; set; } = "comprobante";

    [XmlAttribute("version")]
    public string Version { get; set; } = "1.1.0";

    [XmlElement("infoTributaria")]
    public InfoTributariaXml InfoTributaria { get; set; } = new();

    [XmlElement("infoFactura")]
    public InfoFacturaXml InfoFactura { get; set; } = new();

    // <<< CORRECCIÓN: Se elimina la siguiente lista de aquí.
    // [XmlArray("totalConImpuestos")]
    // [XmlArrayItem("totalImpuesto")]
    // public List<TotalImpuestoXml> TotalConImpuestos { get; set; } = new();

    [XmlArray("detalles")]
    [XmlArrayItem("detalle")]
    public List<DetalleFacturaXml> Detalles { get; set; } = new();
  }

  // ... (El resto de las clases de este archivo pueden quedar igual)


  public class InfoTributariaXml
  {
    public string ambiente { get; set; } = "1";
    public string tipoEmision { get; set; } = "1";
    public string razonSocial { get; set; } = "";
    public string ruc { get; set; } = "";
    public string claveAcceso { get; set; } = "";

    public string codDoc { get; set; } = "01";
    public string estab { get; set; } = "001";
    public string ptoEmi { get; set; } = "001";
    public string secuencial { get; set; } = "000000001";
    public string dirMatriz { get; set; } = "";
  }

  public class InfoFacturaXml
  {
    public string fechaEmision { get; set; } = "";
    public string dirEstablecimiento { get; set; } = "";

    public string tipoIdentificacionComprador { get; set; } = "";
    public string razonSocialComprador { get; set; } = "";
    public string identificacionComprador { get; set; } = "";

    public string totalSinImpuestos { get; set; } = "";
    public string totalDescuento { get; set; } = "";

    [XmlArray("totalConImpuestos")]
    [XmlArrayItem("totalImpuesto")]
    public List<TotalImpuestoXml> TotalConImpuestos { get; set; } = new();

    public string propina { get; set; } = "0.00";
    public string importeTotal { get; set; } = "";
    public string moneda { get; set; } = "USD";
  }

  public class DetalleFacturaXml
  {
    [XmlElement("codigoPrincipal")]
    public string codigoPrincipal { get; set; }

    [XmlElement("descripcion")]
    public string descripcion { get; set; }

    [XmlElement("cantidad")]
    public decimal cantidad { get; set; }

    [XmlElement("precioUnitario")]
    public decimal precioUnitario { get; set; }

    [XmlElement("descuento")]
    public decimal descuento { get; set; }

    [XmlElement("precioTotalSinImpuesto")]
    public decimal precioTotalSinImpuesto { get; set; }

    [XmlArray("impuestos")]
    [XmlArrayItem("impuesto")]
    public List<ImpuestoDetalleXml> impuesto { get; set; }
  }


}
