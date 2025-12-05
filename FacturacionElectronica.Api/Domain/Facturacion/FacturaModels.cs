using System;
using System.Collections.Generic;

namespace FacturacionElectronica.Api.Domain.Facturacion
{
  public class Factura
  {
    public int Id { get; set; }               // PK = columna Id en dbo.Facturas
    public string Numero { get; set; } = "";  // 001-001-000000001
    public DateTime FechaEmision { get; set; } = DateTime.Now;

    public string ClaveAcceso { get; set; } = "";


    // Datos emisor
    public string RucEmisor { get; set; } = "";
    public string RazonSocialEmisor { get; set; } = "";
    public string DireccionMatriz { get; set; } = "";

    // Datos cliente
    public string TipoIdentificacionCliente { get; set; } = "05";
    public string IdentificacionCliente { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string DireccionCliente { get; set; } = "";

    // Totales
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalIva { get; set; }
    public decimal ImporteTotal { get; set; }

    // Relación 1 - N
    public List<FacturaDetalle> Detalles { get; set; } = new();
  }

  public class FacturaDetalle
  {
    // 👇 NUEVO: clave primaria en la tabla FacturaDetalles
    public int FacturaDetalleId { get; set; }

    // 👇 NUEVO: FK hacia Facturas(Id)
    public int FacturaId { get; set; }

    // 👇 NUEVO: navegación hacia Factura
    public Factura? Factura { get; set; }

    // Campos existentes
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal TotalSinImpuesto { get; set; }
    public decimal Iva { get; set; }
  }
}
