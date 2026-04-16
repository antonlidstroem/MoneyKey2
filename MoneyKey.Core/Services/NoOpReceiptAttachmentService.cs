using MoneyKey.Core.Services.Interfaces;

namespace MoneyKey.Core.Services;

public class NoOpReceiptAttachmentService : IReceiptAttachmentService
{
    public Task<string?> UploadAsync(int l, Stream d, string f, string m) => Task.FromResult<string?>(null);
    public Task<string?> GetUrlAsync(int lineId)                          => Task.FromResult<string?>(null);
    public Task          DeleteAsync(int lineId)                          => Task.CompletedTask;
}
