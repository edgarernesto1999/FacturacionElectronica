// using
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

// Servicio para enviar factura por correo
public class EmailService
{
  private readonly IConfiguration _config;

  public EmailService(IConfiguration config)
  {
    _config = config;
  }

  /// <summary>
  /// Envía la factura por correo.
  /// xmlBytes: contenido del XML (obligatorio)
  /// pdfBytes: contenido del PDF (opcional, null si no existe)
  /// toEmail: destinatario (puede contener varias direcciones separadas por ;)
  /// </summary>
  public async Task EnviarFacturaPorCorreoAsync(string toEmail, string nombreCliente, string claveAcceso,
      byte[] xmlBytes, byte[]? pdfBytes = null)
  {
    if (xmlBytes == null || xmlBytes.Length == 0) throw new ArgumentException("El XML es requerido.", nameof(xmlBytes));
    if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentException("El correo destinatario es requerido.", nameof(toEmail));

    var smtpHost = _config["Email:SmtpHost"];
    var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
    var smtpUser = _config["Email:SmtpUser"];
    var smtpPass = _config["Email:SmtpPass"];
    var fromName = _config["Email:FromName"] ?? "Mi Empresa";
    var fromAddress = _config["Email:FromAddress"] ?? smtpUser;

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(fromName, fromAddress));

    // Soportar múltiples destinatarios separados por ';' o ','
    var recipients = toEmail.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var r in recipients) message.To.Add(MailboxAddress.Parse(r.Trim()));

    message.Subject = $"Factura electrónica - Clave: {claveAcceso}";

    // Cuerpo HTML simple
    var bodyBuilder = new BodyBuilder();
    bodyBuilder.HtmlBody = $@"
            <p>Estimado(a) {System.Net.WebUtility.HtmlEncode(nombreCliente)},</p>
            <p>Adjuntamos su factura electrónica (clave <strong>{System.Net.WebUtility.HtmlEncode(claveAcceso)}</strong>).</p>
            <p>Saludos cordiales,<br/>{System.Net.WebUtility.HtmlEncode(fromName)}</p>
        ";

    // Adjuntar el XML
    var xmlFileName = $"factura_{claveAcceso}.xml";
    bodyBuilder.Attachments.Add(xmlFileName, xmlBytes, new ContentType("application", "xml"));

    // Adjuntar PDF si existe
    if (pdfBytes != null && pdfBytes.Length > 0)
    {
      var pdfFileName = $"factura_{claveAcceso}.pdf";
      bodyBuilder.Attachments.Add(pdfFileName, pdfBytes, new ContentType("application", "pdf"));
    }

    message.Body = bodyBuilder.ToMessageBody();

    // Envío SMTP
    using var client = new SmtpClient();
    try
    {
      // Usar conexión segura según tu servidor
      await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTlsWhenAvailable);
      if (!string.IsNullOrEmpty(smtpUser))
      {
        await client.AuthenticateAsync(smtpUser, smtpPass);
      }
      await client.SendAsync(message);
    }
    catch (Exception ex)
    {
      // Manejo de errores: registrar/relanzar según tu política
      // por ejemplo: logger.LogError(ex, "Error enviando correo");
      throw; // o manejar más amigablemente
    }
    finally
    {
      await client.DisconnectAsync(true);
    }
  }
}
