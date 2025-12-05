namespace FacturacionElectronica.Api.DTOs
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
    string Marca,
    string Presentacion,
    bool Activo,
    List<LoteItemDto> Lotes
);
  public class LoteUpdateDto
  {
    public int ProductoId { get; set; }
    public DateOnly FechaCompra { get; set; }
    public DateOnly? FechaExpiracion { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public int CantidadComprada { get; set; }
    public int CantidadDisponible { get; set; }
  }

}
