using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Hunter.Infrastructure.Persistence;

public class HunterDbContextFactory : IDesignTimeDbContextFactory<HunterDbContext>
{
    public HunterDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Hunter.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiProjectPath) ? apiProjectPath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("HunterDb")
            ?? throw new InvalidOperationException("Missing connection string 'HunterDb' for design-time migrations.");

        var optionsBuilder = new DbContextOptionsBuilder<HunterDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new HunterDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }
}
