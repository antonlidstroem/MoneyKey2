using MoneyKey.DAL.Repositories.Interfaces;

namespace MoneyKey.API.Services;

/// <summary>
/// Controls the SignalR feature on/off toggle.
/// A superadmin can disable SignalR to reduce CPU usage when real-time
/// push notifications are not needed.
///
/// The setting is stored in SystemSettings (no FK to Budgets) under key "SignalREnabled".
/// Absent key defaults to enabled for backward compatibility.
/// </summary>
public class SignalRFeatureService
{
    private const string SettingKey = "SignalREnabled";

    private readonly ISystemSettingRepository _repo;

    public SignalRFeatureService(ISystemSettingRepository repo) => _repo = repo;

    public async Task<bool> IsEnabledAsync()
    {
        var value = await _repo.GetAsync(SettingKey);
        // Absent = enabled (backward compatible default)
        return value == null || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetEnabledAsync(bool enabled) =>
        await _repo.SetAsync(SettingKey, enabled ? "true" : "false");
}
