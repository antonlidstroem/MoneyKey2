using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace MoneyKey.API.Services.Email;

public class MailKitEmailService : IEmailService
{
    private readonly EmailOptions               _opts;
    private readonly ILogger<MailKitEmailService> _log;

    public MailKitEmailService(EmailOptions opts, ILogger<MailKitEmailService> log)
    {
        _opts = opts;
        _log  = log;
    }

    public Task SendInviteAsync(string toEmail, string budgetName, string inviteToken, string baseUrl) =>
        SendAsync(toEmail,
            $"Inbjudan till budgeten \"{budgetName}\"",
            $"<p>Du har bjudits in till <strong>{budgetName}</strong>.</p>" +
            $"<p><a href=\"{baseUrl}/accept-invite?token={inviteToken}\" " +
            $"style=\"display:inline-block;padding:10px 20px;background:#1565C0;color:#fff;border-radius:6px;text-decoration:none\">" +
            $"Acceptera inbjudan</a></p>");

    public Task SendReceiptStatusChangedAsync(string toEmail, string batchLabel, string newStatus, string? reason = null)
    {
        var (subj, verb) = newStatus switch
        {
            "Approved"   => ($"Utläggsunderlag godkänt: \"{batchLabel}\"",  "godkändes"),
            "Rejected"   => ($"Utläggsunderlag avslaget: \"{batchLabel}\"", "avslogs"),
            "Reimbursed" => ($"Utlägg utbetalt: \"{batchLabel}\"",          "markerades som utbetalt"),
            _            => ($"Status uppdaterad: \"{batchLabel}\"",        "uppdaterades")
        };
        var body = $"<p>Ditt underlag <strong>\"{batchLabel}\"</strong> {verb}.</p>"
                 + (reason != null ? $"<p><strong>Orsak:</strong> {reason}</p>" : "");
        return SendAsync(toEmail, subj, body);
    }

    public Task SendReceiptSubmittedAsync(string ownerEmail, string submitterEmail, string batchLabel) =>
        SendAsync(ownerEmail,
            $"Nytt utläggsunderlag att granska: \"{batchLabel}\"",
            $"<p>{submitterEmail} har skickat in <strong>\"{batchLabel}\"</strong> för granskning.</p>");

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_opts.SmtpHost))
        {
            _log.LogWarning("SMTP inte konfigurerat. Hoppar över e-post till {To}: {Subject}", toEmail, subject);
            return;
        }
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body    = new TextPart("html")
            {
                Text = "<!DOCTYPE html><html><body style=\"font-family:Arial,sans-serif;color:#37474F;max-width:560px;margin:0 auto;padding:24px\">"
                     + "<div style=\"border-bottom:2px solid #1565C0;padding-bottom:12px;margin-bottom:20px\"><strong style=\"color:#1565C0;font-size:18px\">MoneyKey</strong></div>"
                     + htmlBody + "</body></html>"
            };
            using var client = new SmtpClient();
            await client.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_opts.SmtpUser, _opts.SmtpPass);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex) { _log.LogError(ex, "Misslyckades skicka e-post till {To}", toEmail); }
    }
}
