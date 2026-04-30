using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoneyKey.Blazor.Services.Api;
using MoneyKey.Blazor.Services.Auth;
using MoneyKey.Blazor.Services.Toast;
using MoneyKey.Blazor.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MoneyKey.Blazor.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("sv-SE");

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000";

// ── Auth state provider ───────────────────────────────────────────────────────
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// ── HTTP clients ──────────────────────────────────────────────────────────────
builder.Services.AddTransient<AuthorizationMessageHandler>();

// Main API client — goes through AuthorizationMessageHandler (attaches Bearer token,
// handles 401 by triggering refresh via GetAuthenticationStateAsync).
builder.Services.AddHttpClient("MoneyKeyAPI", client =>
    client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

// Auth-only client — does NOT go through AuthorizationMessageHandler.
// Used exclusively by JwtAuthenticationStateProvider for login/refresh/logout.
// This avoids a circular dependency where a 401 on /refresh would trigger
// another refresh attempt infinitely.
builder.Services.AddHttpClient("MoneyKeyAuth", client =>
    client.BaseAddress = new Uri(apiBase));

// Default scoped HttpClient used by pages and services (MoneyKeyAPI)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("MoneyKeyAPI"));

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorizationCore();

// ── API Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<MilersattningApiService>();
builder.Services.AddScoped<VabApiService>();
builder.Services.AddScoped<ImportApiService>();
builder.Services.AddScoped<ReportsApiService>();
builder.Services.AddScoped<JournalApiService>();
builder.Services.AddScoped<ReceiptApiService>();
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddScoped<ListApiService>();
builder.Services.AddScoped<JobApiService>();
builder.Services.AddScoped<TimeEntryApiService>();
builder.Services.AddScoped<SubscriptionApiService>();
builder.Services.AddScoped<InvitationApiService>();
builder.Services.AddScoped<LoanApiService>();
builder.Services.AddScoped<InsuranceApiService>();
builder.Services.AddScoped<SickLeaveApiService>();
builder.Services.AddScoped<BudgetTargetApiService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<CsnApiService>();

// ── State ─────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<BudgetState>();
builder.Services.AddScoped<JournalFilterState>();
builder.Services.AddScoped<SignalRService>();

await builder.Build().RunAsync();