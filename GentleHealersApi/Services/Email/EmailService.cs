using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GentleHealersApi.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            var (subject, html, text) = EmailTemplates.Build(request);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(request.FirstName, request.ToEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.AppPassword, cancellationToken);
            await client.SendAsync(FormatOptions.Default, message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}