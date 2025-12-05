namespace FacturacionElectronica.Api.DTOs
{
  public class FacturaDto
  {
    // ==============================
    //  DATOS PRINCIPALES
    // ==============================

    /// <summary>Id de la factura (solo se usa al ver, editar, borrar)</summary>
    public int? Id { get; set; }   // null = se está creando

    /// <summary>Ejemplo: 001-001-000000123 (solo lectura; el backend lo genera)</summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>Fecha de emisión, el backend puede asignarla</summary>
    public DateTime? FechaEmision { get; set; }


    // ==============================
    //  DATOS DEL CLIENTE
    // ==============================

    public string TipoIdentificacionCliente { get; set; } = "05"; // cédula por defecto
    public string IdentificacionCliente { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string DireccionCliente { get; set; } = string.Empty;


    // ==============================
    //  DETALLES (PRODUCTOS/SERVICIOS)
    // ==============================

    /// <summary>
    /// Lista de productos/servicios vendidos.
    /// Se pueden mezclar varios tipos.
    /// </summary>
    public List<FacturaDetalleDto> Detalles { get; set; } = new();


    // ==============================
    //  TOTALES (AUTOCALCULADOS)
    // ==============================

    public decimal SubtotalSinImpuestos { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalIva { get; set; }
    public decimal ImporteTotal { get; set; }
  }

  // ========================================
  //  DTO ÚNICO PARA PRODUCTOS DE LA FACTURA
  // ========================================

  public class FacturaDetalleDto
  {
    /// <summary>Id del detalle (solo para actualizar; null cuando se crea)</summary>
    public int? FacturaDetalleId { get; set; }

    /// <summary>Id del producto (opcional)</summary>
    public int? ProductoId { get; set; }

    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }

    /// <summary>Total sin impuestos (solo lectura lo calcula backend)</summary>
    public decimal TotalSinImpuesto { get; set; }

    /// <summary>Iva calculado (solo lectura lo calcula backend)</summary>
    public decimal Iva { get; set; }
  }
}
