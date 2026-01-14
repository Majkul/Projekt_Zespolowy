using ProjektZespolowyGr3.Models;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;

namespace DomPogrzebowyProjekt.Models.System
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            // Jeżeli nie skonfigurowano ustawień maila, pomijamy wysyłkę (np. w Development)
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "No-Reply";
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portString = _configuration["EmailSettings:Port"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(smtpServer) ||
                string.IsNullOrWhiteSpace(portString) ||
                string.IsNullOrWhiteSpace(password))
            {
                // TODO: dodać logowanie informacji, że mail nie został wysłany z powodu braku konfiguracji
                return;
            }

            if (!int.TryParse(portString, out var port))
            {
                // Nieprawidłowy port – też pomijamy wysyłkę w obecnej wersji
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, port, false);
            await client.AuthenticateAsync(senderEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
