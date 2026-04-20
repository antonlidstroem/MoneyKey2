using Microsoft.Data.SqlClient;
using MoneyKey.DAL.Repositories.Interfaces;

namespace MoneyKey.API.Services;

/// <summary>
/// Controls the SignalR feature on/off toggle.
/// A superadmin can disable SignalR to reduce CPU usage when real-time
/// push notifications are not needed.
///
/// The setting is stored in SystemSettings (no FK to Budgets) under key "SignalREnabled".
/// Absent key — OR a missing SystemSettings table (migration not yet applied) — defaults to enabled.
/// </summary>
public class SignalRFeatureService
{
    private const string SettingKey = "SignalREnabled";
    private readonly ISystemSettingRepository _repo;
    public SignalRFeatureService(ISystemSettingRepository repo) => _repo = repo;

    public async Task<bool> IsEnabledAsync()
    {
        try
        {
            var value = await _repo.GetAsync(SettingKey);
            // Absent = enabled (backward compatible default)
            return value == null || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }
        catch (SqlException)
        {
            // Table doesn't exist yet (migration pending) — default to enabled
            return true;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        try
        {
            await _repo.SetAsync(SettingKey, enabled ? "true" : "false");
        }
        catch (SqlException)
        {
            // Table doesn't exist yet — silently ignore
        }
    }
}
