using FacturacionElectronica.Api.Domain.Facturacion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace FacturacionElectronica.Api.Services.Facturacion
{
  public class FacturaPdfService
  {
    public byte[] GenerarPdf(Factura factura)
    {
      var culture = new CultureInfo("es-EC"); // formato numérico/localización

      var doc = Document.Create(container =>
      {
        container.Page(page =>
        {
          page.Size(PageSizes.A4);
          page.Margin(32);
          page.DefaultTextStyle(x => x.FontSize(10));

          // HEADER
          page.Header().PaddingBottom(8).Row(row =>
          {
            // Logo + Empresa
            row.RelativeItem().Column(col =>
            {
              col.Item().Row(r =>
              {
                // Logo placeholder (reemplaza por Image si tienes bytes)
                r.ConstantItem(70).Height(70).AlignCenter().Border(1).Padding(6).Column(logoCol =>
                {
                  logoCol.Item().AlignCenter().Text("LOGO").FontSize(10).SemiBold();
                });

                r.RelativeItem().PaddingLeft(10).Column(company =>
                {
                  company.Item().Text(factura.RazonSocialEmisor)
                         .FontSize(14).SemiBold();
                  company.Item().Text($"RUC: {factura.RucEmisor}").FontSize(10);
                  company.Item().Text(factura.DireccionMatriz).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
              });
            });

            // Invoice box
            row.ConstantItem(260).Column(col =>
            {
              col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(inner =>
              {
                inner.Item().AlignCenter().Text("FACTURA ELECTRÓNICA").FontSize(14).SemiBold();
                inner.Item().PaddingTop(6);
                inner.Item().Row(r =>
                {
                  r.RelativeItem().Column(left =>
                  {
                    left.Item().Text("No:").SemiBold().FontSize(9);
                    left.Item().Text(factura.Numero).FontSize(11);
                  });
                  r.ConstantItem(8);
                  r.RelativeItem().Column(right =>
                  {
                    right.Item().Text("Fecha:").SemiBold().FontSize(9);
                    right.Item().Text($"{factura.FechaEmision:dd/MM/yyyy}").FontSize(11);
                  });
                });
                inner.Item().PaddingTop(6);
                inner.Item().Text("Clave de Acceso:").FontSize(8).SemiBold();
                inner.Item().Text(factura.ClaveAcceso ?? "—").FontSize(8).FontColor(Colors.Blue.Medium);
              });
            });
          });

          // CONTENT
          page.Content().PaddingVertical(8).Column(col =>
          {
            // Datos del cliente y condiciones en una fila
            col.Item().Row(row =>
            {
              row.RelativeItem().Column(cliente =>
              {
                cliente.Item().Border(1).Padding(8).Column(c =>
                {
                  c.Item().Text("Datos del Cliente").SemiBold();
                  c.Item().PaddingTop(4);
                  c.Item().Text($"Nombre: {factura.NombreCliente}");
                  c.Item().Text($"Identificación: {factura.IdentificacionCliente}");
                  c.Item().Text($"Dirección: {factura.DireccionCliente ?? "-"}");
                });
              });

              row.ConstantItem(16);

              row.ConstantItem(260).Column(cond =>
              {
                cond.Item().Border(1).Padding(8).Column(c =>
                {
                  c.Item().Text("Condiciones").SemiBold();
                  c.Item().PaddingTop(4);
                  c.Item().Text($"Moneda: USD");
                  c.Item().Text($"Vendedor: {factura.RazonSocialEmisor}");
                  c.Item().Text($"Forma de Pago: Contado");
                });
              });
            });

            col.Item().PaddingTop(12);

            // Tabla de detalles
            col.Item().Table(table =>
            {
              table.ColumnsDefinition(columns =>
              {
                columns.RelativeColumn(5);
                columns.ConstantColumn(60);
                columns.ConstantColumn(90);
                columns.ConstantColumn(90);
              });

              table.Header(header =>
              {
                header.Cell().Element(HeaderCell).Text("Descripción");
                header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                header.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
              });

              foreach (var d in factura.Detalles)
              {
                table.Cell().Element(BodyCell).Text(d.Descripcion);
                table.Cell().Element(BodyCell).AlignCenter().Text(d.Cantidad.ToString("0.##", culture));
                table.Cell().Element(BodyCell).AlignRight().Text(d.PrecioUnitario.ToString("N2", culture));
                table.Cell().Element(BodyCell).AlignRight().Text(d.TotalSinImpuesto.ToString("N2", culture));
              }

              // Footer row of table could be added here if you need line totals per table.

              static IContainer HeaderCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten3)
                 .Padding(6)
                 .BorderBottom(1)
                 .DefaultTextStyle(x => x.SemiBold().FontSize(10));

              static IContainer BodyCell(IContainer c) =>
                c.Padding(6)
                 .BorderBottom(0.5f)
                 .DefaultTextStyle(x => x.FontSize(9));
            });

            // Totales: alineados a la derecha, con recuadro
            col.Item().PaddingTop(12).AlignRight().Row(r =>
            {
              r.RelativeItem();
              r.ConstantItem(320).Border(1).Padding(10).Column(tot =>
              {
                tot.Item().Row(rt =>
                {
                  rt.RelativeItem().Text("Subtotal").FontSize(10);
                  rt.ConstantItem(100).AlignRight().Text(factura.SubtotalSinImpuestos.ToString("N2", culture)).FontSize(10);
                });

                tot.Item().Row(rt =>
                {
                  rt.RelativeItem().Text("Descuento").FontSize(10);
                  rt.ConstantItem(100).AlignRight().Text(factura.TotalDescuento.ToString("N2", culture)).FontSize(10);
                });

                tot.Item().Row(rt =>
                {
                  rt.RelativeItem().Text($"IVA (15%)").FontSize(10).FontColor(Colors.Grey.Darken1);
                  rt.ConstantItem(100).AlignRight().Text(factura.TotalIva.ToString("N2", culture)).FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                tot.Item().PaddingTop(6).Row(rt =>
                {
                  rt.RelativeItem().Text("TOTAL").FontSize(12).SemiBold();
                  rt.ConstantItem(100).AlignRight().Text(factura.ImporteTotal.ToString("N2", culture)).FontSize(12).SemiBold();
                });
              });
            });

            // Observaciones (opcional)
            col.Item().PaddingTop(12).Text("Observaciones:").SemiBold();
            col.Item().Text("Documento generado electrónicamente. Verifique los datos.").FontSize(9).FontColor(Colors.Grey.Darken1);
          });

          // FOOTER con número de página y nota (invocaciones correctas dentro del delegado)
          page.Footer().AlignCenter().Text(text =>
          {
            text.Span("Documento generado electrónicamente - No requiere firma manuscrita. ");
            text.Span("Página ").SemiBold();
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
          });
        });
      });

      return doc.GeneratePdf();
    }
  }
}
