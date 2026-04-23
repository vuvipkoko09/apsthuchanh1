using System.Net;
using System.Net.Mail;

namespace ConnectDB.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try 
            {
                var smtpHost = _config["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];
                var fromEmail = _config["EmailSettings:FromEmail"];

                Console.WriteLine($"--- Đang chuẩn bị gửi email tới: {to} ---");
                Console.WriteLine($"Sử dụng SMTP: {smtpHost}:{smtpPort}");
                Console.WriteLine($"Tài khoản gửi: {smtpUser}");

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail ?? smtpUser),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("✅ Gửi email THÀNH CÔNG!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ LỖI GỬI EMAIL: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Chi tiết: " + ex.InnerException.Message);
                }
                // Ném lỗi ra để Controller bắt được
                throw;
            }
        }
    }
}
