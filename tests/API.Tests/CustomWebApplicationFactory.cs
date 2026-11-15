using System.Data.Common;
using DataAccess.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtSecretKey = "test-suite-x8Hp3Lm9Vq4Wz7Bn2Kj6Rt1Yc5Fd0Gs9Pa4Me8Uw2Zr7Xq3Nt6Bv";
    private readonly string _connectionString = $"Data Source=VeterinariaInMemory_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("JwtSettings:SecretKey", TestJwtSecretKey);
        builder.UseSetting("JwtSettings:Issuer", "VeterinariaPatitasAPI");
        builder.UseSetting("JwtSettings:Audience", "VeterinariaPatitasClients");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
        builder.UseSetting("RateLimiting:AuthWindowSeconds", "60");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<VeterinariaDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            services.AddDbContext<VeterinariaDbContext>(options =>
            {
                options.UseSqlite(_connectionString)
                       .AddInterceptors(new SqliteSharedMemoryConnectionInterceptor());
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

    private sealed class SqliteSharedMemoryConnectionInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            ConfigureConnection(connection);
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            ConfigureConnection(connection);
            await Task.CompletedTask;
        }

        private static void ConfigureConnection(DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA read_uncommitted = 1; PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }
    }
}
