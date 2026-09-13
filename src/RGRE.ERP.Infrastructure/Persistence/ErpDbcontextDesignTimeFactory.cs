using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RGRE.ERP.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add</c>. Reads the
/// connection string from the Web project's <c>appsettings.json</c> so the
/// tooling can scaffold migrations without a running host.
/// </summary>
public sealed class ErpDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var webProjectDir = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "RGRE.ERP.Web"));

        var config = new ConfigurationBuilder()
            .SetBasePath(webProjectDir)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString =
            config.GetConnectionString("ErpDatabase")
            ?? "Data Source=localhost;Initial Catalog=RGRE_ERP;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var optionsBuilder = new DbContextOptionsBuilder<ErpDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ErpDbContext(optionsBuilder.Options);
    }
}