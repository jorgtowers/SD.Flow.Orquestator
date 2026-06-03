using MailKit.Security;
using MimeKit;
using SD.Flow.Orquestator.Core;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SD.Flow.Orquestator.Actions;

public class SendEmailAction : IWorkflowAction
{
    public string Name => "SendEmail";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "To", "Subject" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
        {

            string to = args["To"];
            string subject = PathHelper.ResolveDynamicPath(args["Subject"]);
            string body = args.GetValueOrDefault("Body", "Cuerpo del mensaje por defecto");

            // Configuración del servidor
            string smtpServer = args.GetValueOrDefault("SmtpServer", "smtp.gmail.com");
            int smtpPort = int.Parse(args.GetValueOrDefault("SmtpPort", "587"));
            string user = args.GetValueOrDefault("SmtpUser", "it.nt.redaccion@gmail.com");
            string pass = args.GetValueOrDefault("SmtpPass", "upyl mnke kbsl ulvn");

            // --- Lógica de Seguridad (SSL/TLS) ---
            // Si el puerto es 465, usamos SslOnConnect (SSL puro). 
            // Si es 587, usamos StartTls. De lo contrario, Auto.
            SecureSocketOptions securityOption = smtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.Auto
            };

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SD Flow", user));
            // 1. Dividimos el string por la coma
            string[] destinatarios = to.Split(',');

            foreach (var direccion in destinatarios)
            {
                // 2. Usamos Trim() para eliminar espacios en blanco accidentales 
                // (ejemplo: "correo1@test.com, correo2@test.com" -> nota el espacio)
                string direccionLimpia = direccion.Trim();

                if (!string.IsNullOrWhiteSpace(direccionLimpia))
                {
                    message.To.Add(new MailboxAddress("", direccionLimpia));
                }
            }
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            // --- Lógica de Adjuntos ---
            if (args.TryGetValue("AttachmentPath", out string? filePath) && !string.IsNullOrEmpty(filePath))
            {
                string path = PathHelper.ResolveDynamicPath(filePath);
                if (File.Exists(path))
                {
                    builder.Attachments.Add(path);
                    WorkflowLogger.Log($"[Email] Adjunto añadido: {Path.GetFileName(path)}");
                }
                else
                {
                    WorkflowLogger.Log($"[Advertencia] No se encontró el adjunto: {path}");
                }
            }

            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    // Conexión usando la opción de seguridad dinámica
                    WorkflowLogger.Log($"[Email] Conectando a {smtpServer}:{smtpPort} usando {securityOption}...");

                    await client.ConnectAsync(smtpServer, smtpPort, securityOption);
                    await client.AuthenticateAsync(user, pass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    WorkflowLogger.Log($"[Email] Correo enviado exitosamente a {to}.");
                }
                catch (Exception ex)
                {
                    WorkflowLogger.Log($"[Error Email] Fallo en el envío: {ex.Message}");
                    throw;
                }
            }
        }
}