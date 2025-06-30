using MamMap.Application.System.Email;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;

    public SmtpEmailSender(IConfiguration config)
    {
        _smtpHost = config["Smtp:Host"];
        _smtpPort = int.Parse(config["Smtp:Port"]);
        _smtpUser = config["Smtp:User"];
        _smtpPass = config["Smtp:Pass"];
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var mail = new MailMessage();
        mail.To.Add(email);
        mail.From = new MailAddress(_smtpUser);
        mail.Subject = subject;
        mail.Body = htmlMessage;
        mail.IsBodyHtml = true;

        using var smtp = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            EnableSsl = true,
        };

        await smtp.SendMailAsync(mail);
    }
}

