using Hangfire;
using LogVue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Heroicsoft.NetForge.Components.Shell;
using ReelSoon.Components;
using ReelSoon.Infrastructure;
using ReelSoon.Services.Catalog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogVue(options =>
    builder.Configuration.GetSection(LogVueOptions.SectionName).Bind(options));

builder.Services.AddSerilog((services, configuration) => configuration
    .MinimumLevel.Information()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.LogVue(services));

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

// Scoped to the circuit: toasts, dialogs and sidebar refreshes stay with the user who caused them.
// The stored key keeps an existing visitor's theme; the system default matches the old behaviour
// of following the OS until a choice is made.
builder.Services.AddNetForgeComponents(o =>
{
    o.ThemeStorageKey = ReelSoon.Constants.ClientThemeLocalStorageKey;
    o.DefaultColorScheme = "system";
    o.AccentColor = "#c026d3";
    o.AccentColorDark = "#e879f9";
});

builder.Services.Configure<ForgeShellOptions>(shell =>
{
    shell.Brand = "ReelSoon";
    shell.BrandHref = "/movies";
    shell.LogoUrl = "img/Logo.png";
    shell.MarkUrl = "img/Icon_Small.png";
    shell.SearchPlaceholder = "Search this list";
    shell.TopbarEndComponent = typeof(ReelSoon.Components.Shell.NotificationBell);
    shell.SidebarFooterComponent = typeof(ReelSoon.Components.Shell.AdminSidebar);
});
builder.Services.AddScoped<IShellNavigation, ReelSoonNavigation>();

builder.Services.AddScoped<AppEvents>();
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

app.MapLogVue().RequireAuthorization(new AuthorizeAttribute
{
    Roles = WellKnownRoles.Administrator
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AdministratorDashboardAuthorizationFilter()],
});

app.Use(async (context, next) =>
{
    // Home.razor also sends "/" to the movie list, but NavigateTo during prerender throws
    // NavigationException and the circuit logs it. Redirect the document request first.
    if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/")
    {
        context.Response.Redirect(MediaKind.Movie.Route());
        return;
    }

    await next(context);
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