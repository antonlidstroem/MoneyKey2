namespace MoneyKey.Core.Services.Interfaces;

public interface IReceiptAttachmentService
{
    Task<string?> UploadAsync(int lineId, Stream data, string fileName, string mimeType);
    Task<string?> GetUrlAsync(int lineId);
    Task          DeleteAsync(int lineId);
}
