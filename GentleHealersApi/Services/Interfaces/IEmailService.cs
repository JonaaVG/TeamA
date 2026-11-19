using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
    }
}