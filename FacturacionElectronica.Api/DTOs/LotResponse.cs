namespace FacturacionElectronica.Api.DTOs;

public record LotResponse(
    int LoteId,
    int ProductoId,
    DateTime FechaCompra,
    DateTime? FechaExpiracion,
    int CantidadComprada,
    int CantidadDisponible,
    decimal CostoUnitario,
    decimal PrecioVentaUnitario,
    int Stock // stock total del producto después de crear el lote
);
