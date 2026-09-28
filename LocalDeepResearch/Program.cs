using LocalDeepResearch.Components;
using LocalDeepResearch.Services;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.Configure<BonsaiOptions>(builder.Configuration.GetSection("Bonsai"));

builder.Services.AddHttpClient<BonsaiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BonsaiOptions>>().Value;
    client.BaseAddress = new Uri(options.Url);
    client.Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
});

builder.Services.Configure<SearxngOptions>(builder.Configuration.GetSection("Searxng"));

builder.Services.AddHttpClient<SearxngClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<SearxngOptions>>().Value;
    client.BaseAddress = new Uri(options.Url);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

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

app.Run();