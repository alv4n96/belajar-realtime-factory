using System.Net;
using System.Net.Sockets;
using EQFR.UI.Components;
using EQFR.UI.Realtime;
using EQFR.UI.Services;
using EQFR.UI.ViewModels;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

var configuredUrls = builder.Configuration[WebHostDefaults.ServerUrlsKey];
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    var resolvedUrls = ResolveAvailableUrls(configuredUrls);
    if (!string.Equals(configuredUrls, resolvedUrls, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[EQFR.UI] Requested URL(s) '{configuredUrls}' in use. Falling back to '{resolvedUrls}'.");
    }

    builder.WebHost.UseUrls(resolvedUrls);
}

builder.Services.Configure<MockBackendOptions>(builder.Configuration.GetSection("MockBackend"));
builder.Services.AddHttpClient("MockBackend", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MockBackendOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<FactorySnapshotStore>();
builder.Services.AddSingleton<SimulationControlService>();
builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddScoped<FactoryConfigService>();
builder.Services.AddHostedService<SimulationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<FactoryHub>("/realtime/factory");

app.Run();

static string ResolveAvailableUrls(string configuredUrls)
{
    var resolved = new List<string>();
    var reservedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var rawUrl in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) || uri.Port <= 0 || !IsLocalHost(uri.Host))
        {
            resolved.Add(rawUrl);
            continue;
        }

        var port = uri.Port;
        while (!IsPortAvailable(uri.Host, port) || !reservedPorts.Add($"{uri.Host}:{port}"))
        {
            port++;
        }

        var uriBuilder = new UriBuilder(uri)
        {
            Port = port,
            Path = string.Empty
        };

        resolved.Add(uriBuilder.Uri.ToString().TrimEnd('/'));
    }

    return string.Join(';', resolved);
}

static bool IsLocalHost(string host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);

static bool IsPortAvailable(string host, int port)
{
    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
    {
        return CanBind(IPAddress.Loopback, port) && CanBind(IPAddress.IPv6Loopback, port);
    }

    if (string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase) || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase))
    {
        return CanBind(IPAddress.IPv6Loopback, port);
    }

    return CanBind(IPAddress.Loopback, port);
}

static bool CanBind(IPAddress address, int port)
{
    try
    {
        using var listener = new TcpListener(address, port);
        listener.Start();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}
