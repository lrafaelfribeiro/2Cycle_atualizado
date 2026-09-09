using API.DTOs;
using API.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace API.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly SMTPSettings _settings;
        public EmailService(IOptions<SMTPSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailASync(EmailRequest emailRequest)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(emailRequest.From);
            mailMessage.To.Add(emailRequest.To);
            mailMessage.Subject = emailRequest.Subject;
            mailMessage.Body = emailRequest.Body;
            mailMessage.IsBodyHtml = emailRequest.IsHtml;

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = _settings.SmtpServer;
            smtpClient.Port = 587;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            smtpClient.EnableSsl = true;
        }
    }
}
