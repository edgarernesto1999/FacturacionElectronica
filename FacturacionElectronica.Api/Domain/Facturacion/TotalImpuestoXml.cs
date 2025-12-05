using System.Xml.Serialization;

namespace FacturacionElectronica.Api.Domain.Facturacion
{

  // using ...

  public class TotalImpuestoXml
  {
    [XmlElement("codigo")]
    public string codigo { get; set; } = "2"; // 2 = IVA

    [XmlElement("codigoPorcentaje")]
    public string codigoPorcentaje { get; set; } 

    [XmlElement("baseImponible")]
    public string baseImponible { get; set; }

    [XmlElement("valor")]
    public string valor { get; set; }
  }

  public class ImpuestoDetalleXml
  {
    public string codigo { get; set; } = "2"; // 2 = IVA

    public string codigoPorcentaje { get; set; }

    public decimal tarifa { get; set; }
    public decimal baseImponible { get; set; }
    public decimal valor { get; set; }
  }
}
