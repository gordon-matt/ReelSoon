using Hangfire;
using Hangfire.Dashboard;
using ReelSoon.Components;
using ReelSoon.Infrastructure;
using ReelSoon.Services.Catalog;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .MinimumLevel.Information()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Synology (and many nginx) reverse proxies close idle HTTP requests at 60s. SignalR's
// long-poll default is 90s, so when WebSockets are blocked the proxy returns an HTML
// error page and the circuit dies. Finish the poll first; the client starts another.
builder.Services.Configure<HttpConnectionDispatcherOptions>(options =>
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(20));

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddScoped<AppEvents>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.SectionName));

string databaseProvider = ReelSoonConfiguration.ResolveDatabaseProvider(builder.Configuration);

builder.Services.AddReelSoonDatabase(builder.Configuration);
builder.Services.AddReelSoonRepositories();
builder.Services.AddReelSoonHangfire(builder.Configuration);

var authProvider = builder.Services.AddReelSoonAuthentication(builder.Configuration);

builder.Services.AddReelSoonServices();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.MapControllers();

if (authProvider == AuthProvider.Identity)
{
    app.MapRazorPages().WithStaticAssets();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider, authProvider, app.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        throw;
    }
}

HangfireRecurringJobs.Register();

Log.Information(
    "ReelSoon {Version} started with the {DatabaseProvider} database provider and {AuthProvider} authentication",
    AppVersion.Current,
    databaseProvider,
    authProvider);

app.Run();
