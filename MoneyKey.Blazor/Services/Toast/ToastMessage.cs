namespace MoneyKey.Blazor.Services.Toast;

public class ToastMessage
{
    public string Id      { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type    { get; set; } = "info";
    public string Icon    { get; set; } = "info";
}
