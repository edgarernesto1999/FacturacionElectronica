using FacturacionElectronica.Api.Data;
using FacturacionElectronica.Api.Domain.Facturacion;
using FacturacionElectronica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FacturacionElectronica.Api.Services.Facturacion
{
  public class FacturaService
  {
    private readonly AppDbContext _context;

    // IVA 15 %
    private const decimal IVA_RATE = 0.15M;

    // Datos de tu RUC (emisor)
    private const string RUC_EMISOR = "1850873736001";
    private const string RAZON_SOCIAL_EMISOR = "IZURIETA ALMENDARIZ EDGAR ERNESTO";
    private const string DIRECCION_MATRIZ =
      "Barrio Yanahurco, Av 25 de Mayo N° 103, Vía a Cevallos, 100 m de la Iglesia, Mocha - Tungurahua";

    public FacturaService(AppDbContext context)
    {
      _context = context;
    }

    // =====================================================
    // CREAR FACTURA
    // =====================================================
    public async Task<Factura> CrearFacturaAsync(FacturaDto dto)
    {
      if (dto.Detalles == null || dto.Detalles.Count == 0)
        throw new InvalidOperationException("La factura debe tener al menos un detalle.");

      var subtotal = dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
      var descuento = dto.Detalles.Sum(d => d.Descuento);
      var subtotalDesc = subtotal - descuento;
      var iva = subtotalDesc * IVA_RATE;
      var total = subtotalDesc + iva;

      var factura = new Factura
      {
        Numero = GenerarSecuencial(),
        FechaEmision = DateTime.Now,

        RucEmisor = RUC_EMISOR,
        RazonSocialEmisor = RAZON_SOCIAL_EMISOR,
        DireccionMatriz = DIRECCION_MATRIZ,

        TipoIdentificacionCliente = dto.TipoIdentificacionCliente,
        IdentificacionCliente = dto.IdentificacionCliente,
        NombreCliente = dto.NombreCliente,
        DireccionCliente = dto.DireccionCliente,

        SubtotalSinImpuestos = subtotalDesc,
        TotalDescuento = descuento,
        TotalIva = iva,
        ImporteTotal = total
      };

      factura.Detalles = dto.Detalles.Select(d => new FacturaDetalle
      {
        Codigo = d.Codigo,
        Descripcion = d.Descripcion,
        Cantidad = d.Cantidad,
        PrecioUnitario = d.PrecioUnitario,
        Descuento = d.Descuento,
        TotalSinImpuesto = (d.Cantidad * d.PrecioUnitario) - d.Descuento,
        Iva = ((d.Cantidad * d.PrecioUnitario) - d.Descuento) * IVA_RATE
      }).ToList();

      _context.Facturas.Add(factura);
      await _context.SaveChangesAsync();
      return factura;
    }

    // =====================================================
    // ACTUALIZAR FACTURA
    // =====================================================
    public async Task<Factura?> ActualizarFacturaAsync(FacturaDto dto)
    {
      if (dto.Id == null)
        return null;

      var factura = await _context.Facturas
        .Include(f => f.Detalles)
        .FirstOrDefaultAsync(f => f.Id == dto.Id.Value);

      if (factura is null)
        return null;

      // Datos del cliente
      factura.TipoIdentificacionCliente = dto.TipoIdentificacionCliente;
      factura.IdentificacionCliente = dto.IdentificacionCliente;
      factura.NombreCliente = dto.NombreCliente;
      factura.DireccionCliente = dto.DireccionCliente;

      if (dto.Detalles == null || dto.Detalles.Count == 0)
        throw new InvalidOperationException("La factura debe tener al menos un detalle.");

      // Eliminar detalles actuales y recrearlos (forma simple)
      _context.FacturaDetalles.RemoveRange(factura.Detalles);

      factura.Detalles = dto.Detalles.Select(d => new FacturaDetalle
      {
        FacturaId = factura.Id,
        Codigo = d.Codigo,
        Descripcion = d.Descripcion,
        Cantidad = d.Cantidad,
        PrecioUnitario = d.PrecioUnitario,
        Descuento = d.Descuento,
        TotalSinImpuesto = (d.Cantidad * d.PrecioUnitario) - d.Descuento,
        Iva = ((d.Cantidad * d.PrecioUnitario) - d.Descuento) * IVA_RATE
      }).ToList();

      // Recalcular totales
      var subtotal = factura.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
      var descuento = factura.Detalles.Sum(d => d.Descuento);
      var subtotalDesc = subtotal - descuento;
      var iva = subtotalDesc * IVA_RATE;
      var total = subtotalDesc + iva;

      factura.SubtotalSinImpuestos = subtotalDesc;
      factura.TotalDescuento = descuento;
      factura.TotalIva = iva;
      factura.ImporteTotal = total;

      await _context.SaveChangesAsync();
      return factura;
    }

    // =====================================================
    // ELIMINAR FACTURA
    // =====================================================
    public async Task<bool> EliminarFacturaAsync(int id)
    {
      var factura = await _context.Facturas
        .Include(f => f.Detalles)
        .FirstOrDefaultAsync(f => f.Id == id);

      if (factura is null)
        return false;

      // Gracias al OnDelete.Cascade en AppDbContext basta con eliminar la factura
      _context.Facturas.Remove(factura);
      await _context.SaveChangesAsync();
      return true;
    }

    // =====================================================
    // PRIVADO: GENERAR SECUENCIAL
    // =====================================================
    private string GenerarSecuencial()
    {
      var count = _context.Facturas.Count() + 1;
      return $"001-001-{count.ToString().PadLeft(9, '0')}";
    }
  }
}
