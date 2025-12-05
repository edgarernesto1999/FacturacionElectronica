using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FacturacionElectronica.Api.Domain.Facturacion;

namespace FacturacionElectronica.Api.Services.Facturacion
{
  public class FacturaXmlService
  {
    public FacturaXml MapToXmlModel(Factura factura)
    {
      // Cultura invariante para asegurar que el separador decimal sea '.'
      var ci = CultureInfo.InvariantCulture;

      // ==================================================================
      // AJUSTE PARA EL AMBIENTE DE PRUEBAS DEL SRI (ahora usando 15%)
      // ==================================================================
      const decimal IVA_RATE_PARA_PRUEBAS = 0.15M; // cambiado a 15%
      const string CODIGO_PORCENTAJE_PARA_PRUEBAS = "4"; // código para 15%
      // ==================================================================

      var partes = (factura.Numero ?? "").Split('-');
      var estab = partes.Length > 0 ? partes[0] : "001";
      var ptoEmi = partes.Length > 1 ? partes[1] : "001";
      var secuencial = partes.Length > 2 ? partes[2] : "000000001";

      factura.ClaveAcceso = string.IsNullOrWhiteSpace(factura.ClaveAcceso)
          ? GenerarClaveAcceso(factura, estab, ptoEmi, secuencial)
          : factura.ClaveAcceso;

      var infoTrib = new InfoTributariaXml
      {
        ambiente = "1",
        tipoEmision = "1",
        razonSocial = factura.RazonSocialEmisor,
        ruc = factura.RucEmisor,
        claveAcceso = factura.ClaveAcceso,
        codDoc = "01",
        estab = estab,
        ptoEmi = ptoEmi,
        secuencial = secuencial.PadLeft(9, '0'),
        dirMatriz = factura.DireccionMatriz
      };

      var totalConImpuestos = new List<TotalImpuestoXml>();
      if (factura.TotalIva > 0)
      {
        totalConImpuestos.Add(new TotalImpuestoXml
        {
          codigo = "2",
          codigoPorcentaje = CODIGO_PORCENTAJE_PARA_PRUEBAS,
          // --- CORRECCIÓN: Convertir decimal a string con formato ---
          baseImponible = factura.Detalles.Where(d => d.Iva > 0).Sum(d => d.TotalSinImpuesto).ToString("0.00", ci),
          // si tu TotalImpuestoXml tiene campo tarifa, podrías asignar "15.00" aquí
          valor = factura.TotalIva.ToString("0.00", ci)
        });
      }

      AjustarDatosClienteSegunSri(factura);

      var infoFac = new InfoFacturaXml
      {
        fechaEmision = factura.FechaEmision.ToString("dd/MM/yyyy"),
        dirEstablecimiento = factura.DireccionMatriz,
        tipoIdentificacionComprador = factura.TipoIdentificacionCliente,
        razonSocialComprador = factura.NombreCliente,
        identificacionComprador = factura.IdentificacionCliente,
        totalSinImpuestos = factura.SubtotalSinImpuestos.ToString("0.00", ci),
        totalDescuento = factura.TotalDescuento.ToString("0.00", ci),
        TotalConImpuestos = totalConImpuestos,
        propina = "0.00",
        importeTotal = factura.ImporteTotal.ToString("0.00", ci),
        moneda = "USD"
      };

      var detalles = factura.Detalles.Select(d =>
      {
        var impuestoDetalle = new ImpuestoDetalleXml { codigo = "2" };
        if (d.Iva > 0)
        {
          impuestoDetalle.codigoPorcentaje = CODIGO_PORCENTAJE_PARA_PRUEBAS;
          // tarifa en porcentaje (ej. 15), si el campo es decimal puede quedar 15.00
          impuestoDetalle.tarifa = IVA_RATE_PARA_PRUEBAS * 100;
          impuestoDetalle.baseImponible = d.TotalSinImpuesto;
          impuestoDetalle.valor = d.Iva;
        }
        else
        {
          impuestoDetalle.codigoPorcentaje = "0";
          impuestoDetalle.tarifa = 0;
          impuestoDetalle.baseImponible = d.TotalSinImpuesto;
          impuestoDetalle.valor = 0;
        }
        return new DetalleFacturaXml
        {
          codigoPrincipal = d.Codigo,
          descripcion = d.Descripcion,
          cantidad = d.Cantidad,
          precioUnitario = d.PrecioUnitario,
          descuento = d.Descuento,
          precioTotalSinImpuesto = d.TotalSinImpuesto,
          impuesto = new List<ImpuestoDetalleXml> { impuestoDetalle }
        };
      }).ToList();

      return new FacturaXml
      {
        InfoTributaria = infoTrib,
        InfoFactura = infoFac,
        Detalles = detalles
      };
    }

    // ... (GenerarClaveAcceso, CalcularDigitoVerificador, etc. sin cambios) ...

    private void AjustarDatosClienteSegunSri(Factura factura)
    {
      var tipo = ObtenerCodigoTipoIdentificacion(factura.TipoIdentificacionCliente);
      factura.TipoIdentificacionCliente = tipo;
      if (tipo == "07")
      {
        factura.IdentificacionCliente = "9999999999999";
        factura.NombreCliente = "CONSUMIDOR FINAL";
      }
    }

    private string ObtenerCodigoTipoIdentificacion(string tipo)
    {
      if (string.IsNullOrWhiteSpace(tipo)) return "07";
      tipo = tipo.Trim().ToUpper();
      return tipo switch
      {
        "RUC" => "04",
        "CEDULA" or "CÉDULA" => "05",
        "PASAPORTE" => "06",
        "CONSUMIDOR FINAL" => "07",
        "EXTERIOR" or "IDENTIFICACION DEL EXTERIOR" => "08",
        _ => "07"
      };
    }

    private string GenerarClaveAcceso(Factura factura, string estab, string ptoEmi, string secuencial)
    {
      var fecha = factura.FechaEmision.ToString("ddMMyyyy");
      var codDoc = "01";
      var ruc = factura.RucEmisor;
      var ambiente = "1";
      var serie = estab + ptoEmi;
      var sec = secuencial.PadLeft(9, '0');
      var codigoNumerico = "12345678";
      var tipoEmision = "1";
      var baseClave = fecha + codDoc + ruc + ambiente + serie + sec + codigoNumerico + tipoEmision;
      var digito = CalcularDigitoVerificador(baseClave);
      return baseClave + digito;
    }

    private int CalcularDigitoVerificador(string claveSinDigito)
    {
      int[] pesos = { 2, 3, 4, 5, 6, 7 };
      int suma = 0;
      int pesoIndex = 0;
      for (int i = claveSinDigito.Length - 1; i >= 0; i--)
      {
        int digito = claveSinDigito[i] - '0';
        suma += digito * pesos[pesoIndex];
        pesoIndex++;
        if (pesoIndex >= pesos.Length)
        {
          pesoIndex = 0;
        }
      }
      int modulo = suma % 11;
      int digitoVerificador = 11 - modulo;
      if (digitoVerificador == 11) { return 0; }
      if (digitoVerificador == 10) { return 1; }
      return digitoVerificador;
    }

    public byte[] GenerarXml(Factura factura)
    {
      var facturaXml = MapToXmlModel(factura);
      var serializer = new XmlSerializer(typeof(FacturaXml));
      var settings = new XmlWriterSettings
      {
        Encoding = new UTF8Encoding(false),
        Indent = true,
        OmitXmlDeclaration = false
      };
      using var ms = new MemoryStream();
      using (var writer = XmlWriter.Create(ms, settings))
      {
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");
        serializer.Serialize(writer, facturaXml, ns);
      }
      return ms.ToArray();
    }
  }
}
