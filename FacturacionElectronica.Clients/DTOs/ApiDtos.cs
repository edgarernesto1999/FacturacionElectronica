using System;
using System.Collections.Generic;

namespace FacturacionElectronica.Clients.Data
{
  // ... (las otras clases no cambian) ...
  public class CrearProductoRequest { /* ... */ }
  public class ProductoResponse { /* ... */ }
  public class ProductoListadoDto { /* ... */ }
  public class ApiResponse<T> { /* ... */ }


  // --- DTOs para Lotes ---
  public class CrearLoteRequest
  {
    public int ProductId { get; set; }

    // ==================================================================
    // === ESTE ES EL CAMBIO PRINCIPAL                                ===
    // === Cambiamos DateTime por DateOnly para coincidir con la API  ===
    // ==================================================================
    public DateOnly FechaCompra { get; set; }

    public DateTime? FechaExpiracion { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public int CantidadComprada { get; set; }
  }
}
