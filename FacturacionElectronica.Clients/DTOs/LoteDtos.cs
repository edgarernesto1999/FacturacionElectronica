namespace FacturacionElectronica.Clients.DTOs
{

  public record LoteCreateDto(
    int ProductId,
    DateOnly FechaCompra,
    DateOnly? FechaExpiracion,
    decimal CostoUnitario,
    decimal PrecioVentaUnitario,
    int CantidadComprada
    );
  public record LoteItemDto(
    int LoteId,
    DateOnly FechaCompra,
    DateOnly? FechaExpiracion,
    decimal CostoUnitario,
    decimal PrecioVentaUnitario,
    int CantidadComprada,
    int CantidadDisponible
);
  public record ProductWithLotesDto(
    int ProductoId,
    string TipoProducto,
    string Nombre,
    bool Activo,
    List<LoteItemDto> Lotes
);
}
