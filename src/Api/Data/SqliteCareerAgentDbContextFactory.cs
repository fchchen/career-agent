using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareerAgent.Api.Data;

public sealed class SqliteCareerAgentDbContextFactory : IDesignTimeDbContextFactory<CareerAgentDbContext>
{
    public CareerAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CareerAgentDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Sqlite");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=career-agent.design.db";
        }

        optionsBuilder.UseSqlite(connectionString);
        return new CareerAgentDbContext(optionsBuilder.Options);
    }
}
