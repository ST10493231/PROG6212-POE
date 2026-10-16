using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Database;

namespace RaceDay.Tests.TestHelpers;

/// Spins up the real RaceDay API pipeline (controllers, session-based authentication,
/// authorization policies included) against an isolated in-memory database, so tests
/// exercise actual HTTP requests and the real [Authorize(Roles = ...)] enforcement
/// rather than calling controller methods directly. HttpClients created from this
/// factory handle cookies by default, so a login's session cookie is retained and
/// resent automatically for the lifetime of that client.
///
/// UseEnvironment("Testing") makes Program.cs skip its own UseSqlServer registration
/// entirely (see Program.cs), so this is the *only* place RaceDayDbContext gets a
/// provider for test runs -- no SqlServer-vs-InMemory conflict to clean up.

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();
        });
    }
}