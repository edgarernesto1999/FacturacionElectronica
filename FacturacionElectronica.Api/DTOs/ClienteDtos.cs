namespace FacturacionElectronica.Api.DTOs
{
  // DTO para crear un nuevo cliente
  public record ClienteCreateDto(
      string Cedula,
      string Nombre,
      string Apellido,
      string? Direccion,
      string? Correo,
      string? Telefono
  );

  // DTO para actualizar un cliente existente
  public record ClienteUpdateDto(
      string Nombre,
      string Apellido,
      string? Direccion,
      string? Correo,
      string? Telefono
  );

  // DTO para mostrar en lista de clientes
  public record ClienteListItemDto(
      string Cedula,
      string Nombre,
      string Apellido,
      string? Correo,
      string? Telefono
  );

  // DTO para mostrar detalles (por ejemplo para facturación)
  public record ClienteDetailDto(
      string Cedula,
      string Nombre,
      string Apellido,
      string? Direccion,
      string? Correo,
      string? Telefono
  );
}
