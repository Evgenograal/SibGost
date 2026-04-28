using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace ClientApp.Services
{
    public class EmailService
    {
        private const string SenderEmail = "Evgenograal@yandex.ru";
        private const string SenderPassword = "fixywtolwyctsbve";

        public static async Task<bool> SendCode(string toEmail, string code)
        {
            try
            {   
                var smtp = new SmtpClient("smtp.yandex.ru", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(SenderEmail, SenderPassword)
                };

                var msg = new MailMessage
                {
                    From = new MailAddress(SenderEmail, "SibGost App"),
                    Subject = "Код подтверждения регистрации",
                    Body = $"Ваш код подтверждения для SibGost: <b>{code}</b>",
                    IsBodyHtml = true
                };
                msg.To.Add(toEmail);

                await smtp.SendMailAsync(msg);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка Email: {ex.Message}");
                return false;
            }
        }
    }
}
