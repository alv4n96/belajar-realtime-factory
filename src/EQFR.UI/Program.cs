using EQFR.UI.Components;
using EQFR.UI.Realtime;
using EQFR.UI.Services;
using EQFR.UI.ViewModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<FactorySnapshotStore>();
builder.Services.AddSingleton<SimulationControlService>();
builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddHostedService<SimulationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
