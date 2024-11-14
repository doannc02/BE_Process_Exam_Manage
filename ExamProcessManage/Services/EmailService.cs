using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace ExamProcessManage.Services;

public static class EmailService
{
    public static async void SendEmail(string subject, string body, string toEmail)
    {
        try
        {
            string? fromEmail, password;

            var jsonString = await File.ReadAllTextAsync("./appsettings.json");

            using (var doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;

                fromEmail = root.GetProperty("Gmail").GetProperty("Address").GetString();
                password = root.GetProperty("Gmail").GetProperty("Password").GetString();
            }

            if (fromEmail == null || password == null) return;

            var fromAddress = new MailAddress(fromEmail, "VIU-EPM");
            var toAddress = new MailAddress(toEmail);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress.Address, password),
                Timeout = 20000
            };

            using var email = new MailMessage(fromAddress, toAddress);
            email.IsBodyHtml = true;
            email.Subject = subject;
            email.Body = body;

            smtp.Send(email);
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}