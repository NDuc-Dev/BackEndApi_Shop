using AdminApi.DTOs.Email;

namespace AdminApi.Interfaces
{
    public interface IEmailServices
    {
        Task<bool> SendEmail(EmailSendDto emailSend);
    }
}