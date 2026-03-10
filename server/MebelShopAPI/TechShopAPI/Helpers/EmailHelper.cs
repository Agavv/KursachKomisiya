using MimeKit;
using MailKit.Net.Smtp;

namespace 
    API.Helpers
{
    public class EmailHelper
    {
        private readonly string smtpServer = "smtp.mail.ru";
        private readonly int smtpPort = 587;
        private readonly string emailFrom = "mebelmpt@mail.ru";
        private readonly string password = "fAPc1kImdU55SrsoM21T";

        public async Task SendEmail(string toEmail, string subject, string body, string htmlBody = null)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Магазин мебели MebelShop", emailFrom));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = body,
                HtmlBody = htmlBody ?? body
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpServer, smtpPort, false);
                await client.AuthenticateAsync(emailFrom, password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
