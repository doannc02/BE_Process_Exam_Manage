using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

namespace ExamProcessManage.Services
{
    public static class EmailService
    {
        private static string fromEmail;
        private static string password;
        private static string smtpHost;
        private static int smtpPort;

        // Khởi tạo cấu hình trong constructor tĩnh
        static EmailService()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Lấy thư mục hiện tại
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Đọc cấu hình từ appsettings.json
            fromEmail = configuration["EmailSettings:SmtpUsername"];
            password = configuration["EmailSettings:SmtpPassword"];
            smtpHost = configuration["EmailSettings:SmtpHost"];
            smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
        }

        // Phương thức gửi email
        public static async Task SendEmail(string subject, string body, string toEmail)
        {
            try
            {
                if (fromEmail == null || password == null)
                    throw new InvalidOperationException("Email or password is missing from configuration");

                var fromAddress = new MailAddress(fromEmail, "VIU-EPM");
                var toAddress = new MailAddress(toEmail);

                var smtp = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(fromAddress.Address, password),
                    Timeout = 20000
                };

                using var email = new MailMessage(fromAddress, toAddress)
                {
                    IsBodyHtml = true,
                    Subject = subject,
                    Body = body
                };

                await smtp.SendMailAsync(email);
            }
            catch (Exception e)
            {
                // Ghi log hoặc thông báo lỗi
                Console.WriteLine($"Error sending email: {e.Message}");
                throw;
            }
        }
    }
}
