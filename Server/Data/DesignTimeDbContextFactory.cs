using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Server.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build config
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Force SQLite for design-time
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var provider = config.GetValue<string>("DatabaseProvider") ?? "Sqlite";
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlite(config.GetConnectionString("SqliteConnection"));
        }
        else
        {
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
        }
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}