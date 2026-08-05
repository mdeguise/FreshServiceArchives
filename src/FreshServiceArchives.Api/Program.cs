using FreshServiceArchives.Api.Data;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// --- Windows Authentication ---------------------------------------------------
// Negotiate is the default scheme so a Kestrel `dotnet run` can authenticate for
// local dev. When hosted in IIS (in-process), the ASP.NET Core Module performs the
// Windows/AD handshake and the Negotiate handler defers to it — same code, both hosts.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // Every endpoint requires an authenticated Windows user unless it opts out.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<IISServerOptions>(o => o.AutomaticAuthentication = true);

// --- Application services -----------------------------------------------------
builder.Services.Configure<AccessOptions>(builder.Configuration.GetSection(AccessOptions.SectionName));
builder.Services.Configure<ArchiveOptions>(builder.Configuration.GetSection(ArchiveOptions.SectionName));
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddSingleton<IDirectorySearchService, DirectorySearchService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve the built React SPA (wwwroot) as the site root. These run before
// UseRouting so the static-file middleware serves index.html/assets directly
// (an anonymous shell); the SPA then calls the auth-protected /api endpoints.
app.UseDefaultFiles();
app.UseStaticFiles();

// Call UseRouting explicitly *after* static files. Otherwise the framework
// auto-inserts it at the top of the pipeline, the SPA fallback endpoint matches
// "/" first, and the static-file middleware skips requests that already have an
// endpoint — leaving "/" gated behind authorization (401) instead of serving.
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback: any non-API, non-file route returns index.html so client-side
// routing (react-router) works on deep links and refreshes. Anonymous so the
// shell always loads; the SPA's /api calls are what enforce Windows auth.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();
