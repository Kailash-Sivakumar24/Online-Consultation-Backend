using System;
using System.Linq;
using ConsultationApi.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ConsultationApi.Tests.Integration;

public class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    protected HttpClient Client { get; private set; } = null!;
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // If INTEGRATION_DB is set, use that connection string and skip Docker.
        var envConn = Environment.GetEnvironmentVariable("INTEGRATION_DB");

        if (string.IsNullOrWhiteSpace(envConn))
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
        }

        var connectionStringToUse = envConn ?? _postgres!.GetConnectionString();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Testing");
                host.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(connectionStringToUse));

                    using var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_postgres != null)
            await _postgres.StopAsync();

        if (Factory != null)
            await Factory.DisposeAsync();
    }
}
