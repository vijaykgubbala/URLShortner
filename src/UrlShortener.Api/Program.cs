var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// No routes yet. The scaffold route left by `dotnet new web` was removed: it was
// `app.MapGet("/", ...)`, which STD-ARCH-03 and architecture/api.md §2.1 forbid —
// "A route with no version prefix must not be added." It was the only route in the
// repository, so leaving it invited the next endpoint to be copied from it.
//
// Endpoints arrive with #18 (create), #19 (redirect) and #22/#24 (query, update),
// at which point this file also becomes the composition root layers.md §2.3 describes
// and registers DnsHostResolver against IHostResolver — see ARCH-001.

app.Run();
