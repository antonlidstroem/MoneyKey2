namespace MoneyKey.Blazor.Services.Toast;

public class ToastService
{
    public List<ToastMessage> Toasts { get; } = new();
    public event Action? OnChange;

    public void Show(string message, string type = "info", int durationMs = 4000)
    {
        var id = Guid.NewGuid().ToString();
        Toasts.Add(new ToastMessage
        {
            Id      = id,
            Message = message,
            Type    = type,
            Icon    = type switch { "success" => "check_circle", "error" => "error", _ => "info" }
        });
        OnChange?.Invoke();
        _ = Task.Delay(durationMs).ContinueWith(_ => { Remove(id); });
    }

    public void Success(string m) => Show(m, "success");
    public void Error(string m)   => Show(m, "error");
    public void Info(string m)    => Show(m, "info");

    public void Remove(string id)
    {
        Toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
