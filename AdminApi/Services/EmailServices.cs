using AdminApi.DTOs.Email;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;

namespace AdminApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendEmail(EmailSendDto emailSend)
        {
            MailjetClient mailjet = new MailjetClient(_config["MailJet:APIKey"], _config["MailJet:SecretKey"]);
            var email = new TransactionalEmailBuilder().WithFrom(new SendContact(_config["Email:From"], _config["Email:ApplicationName"]))
                .WithSubject(emailSend.Subject)
                .WithHtmlPart(emailSend.Body)
                .WithTo(new SendContact(emailSend.To))
                .Build();

            var response = await mailjet.SendTransactionalEmailAsync(email);
            if (response.Messages != null)
            {
                if (response.Messages[0].Status == "success")
                {
                    return true;
                }
            }
            return false;
        }
    }
}