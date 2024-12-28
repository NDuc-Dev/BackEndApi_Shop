using UserApi.DTOs.Email;

namespace UserApi.Interfaces
{
    public interface IEmailServices
    {
        Task<bool> SendEmail(EmailSendDto emailSend);
    }
}