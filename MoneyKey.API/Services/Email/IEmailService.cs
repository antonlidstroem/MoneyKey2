namespace MoneyKey.API.Services.Email;

public interface IEmailService
{
    Task SendInviteAsync(string toEmail, string budgetName, string inviteToken, string baseUrl);
    Task SendReceiptStatusChangedAsync(string toEmail, string batchLabel, string newStatus, string? reason = null);
    Task SendReceiptSubmittedAsync(string ownerEmail, string submitterEmail, string batchLabel);
}
