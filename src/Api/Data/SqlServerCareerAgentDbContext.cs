using Microsoft.EntityFrameworkCore;

namespace CareerAgent.Api.Data;

public sealed class SqlServerCareerAgentDbContext : CareerAgentDbContext
{
    public const string SchemaName = "career";

    public SqlServerCareerAgentDbContext(DbContextOptions<SqlServerCareerAgentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);
    }
}
