using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.ShortLinks;
using UrlShortener.Application.Destinations;
using UrlShortener.Application.ShortLinks;
using UrlShortener.Domain.ShortLinks;
using UrlShortener.Infrastructure.Dns;
using UrlShortener.Infrastructure.ShortLinks;

// The composition root — layers.md §2.3. The only file permitted to reference every
// layer, and only to bind implementations to interfaces. This is also what earns the
// Infrastructure project reference that §2.2 otherwise forbids (review finding ARCH-001).

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShortLinkDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("ShortLinks") ?? "Data Source=shortlinks.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IShortCodeGenerator, CryptoShortCodeGenerator>();
builder.Services.AddSingleton<ILinkTokenGenerator, CryptoLinkTokenGenerator>();
builder.Services.AddSingleton<ILinkTokenVerifier, LinkTokenVerifier>();

// ADR-001: explicit timeout, no retry.
builder.Services.AddSingleton<IHostResolver>(_ => new DnsHostResolver(TimeSpan.FromSeconds(2)));

// Singletons: their tallies are process-wide and meaningless per request.
builder.Services.AddSingleton<RejectionCounter>();
builder.Services.AddSingleton<CreateFailureCounter>();
builder.Services.AddSingleton<ResolveFailureCounter>();

builder.Services.AddScoped<IShortLinkRepository, EfShortLinkRepository>();
builder.Services.AddScoped<ValidateDestination>();
builder.Services.AddScoped<CreateShortLink>();
builder.Services.AddScoped<ResolveShortLink>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ShortLinkDbContext>().Database.EnsureCreated();
}

app.MapShortLinks();

app.Run();

/// <summary>Exposed so the integration tests can host this application.</summary>
public partial class Program;
