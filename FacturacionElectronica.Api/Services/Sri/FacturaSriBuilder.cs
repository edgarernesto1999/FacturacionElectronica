using System.Text;
using System.Xml;
using System.Globalization;
using FacturacionElectronica.Api.DTOs;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace FacturacionElectronica.Api.Services.Sri
{
  public class FacturaSriBuilder
  {
    private readonly IConfiguration _config;

    public FacturaSriBuilder(IConfiguration config)
    {
      _config = config;
    }

    public string GenerarXmlFactura(FacturaDto factura)
    {
      if (!factura.FechaEmision.HasValue)
      {
        throw new ArgumentNullException(nameof(factura.FechaEmision), "La fecha de emisión no puede ser nula para generar el XML.");
      }
      var rucEmisor = _config["Sri:RucEmisor"] ?? "9999999999999";
      var razonSocial = _config["Sri:RazonSocialEmisor"] ?? "EMISOR DEMO";
      var ambiente = _config["Sri:Ambiente"] ?? "1";
      var tipoEmision = _config["Sri:TipoEmision"] ?? "1";
      var claveAcceso = "0123456789012345678901234567890123456789012345678"; // Dummy
      var culture = new CultureInfo("en-US");

      var sb = new StringBuilder();
      using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
      {
        xw.WriteStartElement("factura");
        xw.WriteAttributeString("id", "comprobante");
        xw.WriteAttributeString("version", "1.1.0");

        xw.WriteStartElement("infoTributaria");
        xw.WriteElementString("ambiente", ambiente);
        xw.WriteElementString("tipoEmision", tipoEmision);
        xw.WriteElementString("razonSocial", razonSocial);
        xw.WriteElementString("nombreComercial", razonSocial);
        xw.WriteElementString("ruc", rucEmisor);
        xw.WriteElementString("claveAcceso", claveAcceso);
        xw.WriteElementString("codDoc", "01");
        xw.WriteElementString("estab", factura.Numero.Substring(0, 3));
        xw.WriteElementString("ptoEmi", factura.Numero.Substring(4, 3));
        xw.WriteElementString("secuencial", factura.Numero.Substring(8));
        xw.WriteElementString("dirMatriz", "DIRECCION DEMO");
        xw.WriteEndElement();

        xw.WriteStartElement("infoFactura");
        xw.WriteElementString("fechaEmision", factura.FechaEmision.Value.ToString("dd/MM/yyyy"));
        xw.WriteElementString("dirEstablecimiento", "DIRECCION ESTABLECIMIENTO");
        xw.WriteElementString("obligadoContabilidad", "NO");
        xw.WriteElementString("tipoIdentificacionComprador", factura.TipoIdentificacionCliente);
        xw.WriteElementString("razonSocialComprador", factura.NombreCliente);
        xw.WriteElementString("identificacionComprador", factura.IdentificacionCliente);
        xw.WriteElementString("totalSinImpuestos", factura.SubtotalSinImpuestos.ToString("F2", culture));
        xw.WriteElementString("totalDescuento", factura.TotalDescuento.ToString("F2", culture));

        // --- totalConImpuestos ---
        xw.WriteStartElement("totalConImpuestos");
        if (factura.TotalIva > 0)
        {
          xw.WriteStartElement("totalImpuesto");
          xw.WriteElementString("codigo", "2"); // 2 = IVA
          // Usar el código de porcentaje para IVA 15%
          xw.WriteElementString("codigoPorcentaje", "4"); // 4 = IVA 15%
          xw.WriteElementString("baseImponible", factura.Detalles.Where(d => d.Iva > 0).Sum(d => d.TotalSinImpuesto).ToString("F2", culture));
          // Usar la tarifa correcta del 15%
          xw.WriteElementString("tarifa", "15.00");
          xw.WriteElementString("valor", factura.TotalIva.ToString("F2", culture));
          xw.WriteEndElement(); // totalImpuesto
        }
        xw.WriteEndElement(); // totalConImpuestos

        xw.WriteElementString("propina", "0.00");
        xw.WriteElementString("importeTotal", factura.ImporteTotal.ToString("F2", culture));
        xw.WriteElementString("moneda", "DOLAR");
        xw.WriteEndElement(); // infoFactura

        // =============== detalles ===============
        xw.WriteStartElement("detalles");
        foreach (var detalle in factura.Detalles)
        {
          xw.WriteStartElement("detalle");
          xw.WriteElementString("codigoPrincipal", detalle.Codigo);
          xw.WriteElementString("descripcion", detalle.Descripcion);
          xw.WriteElementString("cantidad", detalle.Cantidad.ToString("F6", culture));
          xw.WriteElementString("precioUnitario", detalle.PrecioUnitario.ToString("F6", culture));
          xw.WriteElementString("descuento", detalle.Descuento.ToString("F2", culture));
          xw.WriteElementString("precioTotalSinImpuesto", detalle.TotalSinImpuesto.ToString("F2", culture));

          xw.WriteStartElement("impuestos");
          xw.WriteStartElement("impuesto");
          xw.WriteElementString("codigo", "2"); // 2 = IVA

          string codigoPorcentajeDetalle;
          string tarifaDetalle;
          if (detalle.Iva > 0)
          {
            // Usar código y tarifa del 15% también en los detalles
            codigoPorcentajeDetalle = "4"; // Código para 15%
            tarifaDetalle = "15.00";
          }
          else
          {
            codigoPorcentajeDetalle = "0"; // 0 = IVA 0%
            tarifaDetalle = "0.00";
          }

          xw.WriteElementString("codigoPorcentaje", codigoPorcentajeDetalle);
          xw.WriteElementString("tarifa", tarifaDetalle);
          xw.WriteElementString("baseImponible", detalle.TotalSinImpuesto.ToString("F2", culture));
          xw.WriteElementString("valor", detalle.Iva.ToString("F2", culture));
          xw.WriteEndElement(); // impuesto
          xw.WriteEndElement(); // impuestos
          xw.WriteEndElement(); // detalle
        }
        xw.WriteEndElement(); // detalles
        xw.WriteEndElement(); // factura
      }
      return sb.ToString();
    }
  }
}
