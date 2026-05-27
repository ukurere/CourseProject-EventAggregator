using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EventAggregator.API.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = _config["Smtp:Host"];
        var username = _config["Smtp:Username"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogInformation("[EMAIL STUB] To: {To} | Subject: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Smtp:FromName"] ?? "EventAggregator",
            _config["Smtp:FromEmail"] ?? username));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            int.Parse(_config["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, _config["Smtp:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
