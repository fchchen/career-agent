using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareerAgent.Api.Data;

public sealed class SqlServerCareerAgentDbContextFactory : IDesignTimeDbContextFactory<SqlServerCareerAgentDbContext>
{
    public SqlServerCareerAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerCareerAgentDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Database=CareerAgentDesignTime;User Id=sa;Password=StrongPassword!123;Encrypt=False;TrustServerCertificate=True";
        }

        optionsBuilder.UseSqlServer(connectionString);
        return new SqlServerCareerAgentDbContext(optionsBuilder.Options);
    }
}
