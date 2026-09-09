using API.DTOs;

namespace API.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailASync(EmailRequest request);
    }
}
