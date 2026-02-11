using System.Text.Json;
using CareerAgent.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CareerAgent.Api.Data;

public class CareerAgentDbContext : DbContext
{
    public CareerAgentDbContext(DbContextOptions<CareerAgentDbContext> options) : base(options) { }

    public DbSet<JobListing> JobListings => Set<JobListing>();
    public DbSet<MasterResume> MasterResumes => Set<MasterResume>();
    public DbSet<TailoredDocument> TailoredDocuments => Set<TailoredDocument>();
    public DbSet<SearchProfile> SearchProfiles => Set<SearchProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ExternalId, e.Source }).IsUnique();
            entity.HasIndex(e => e.RelevanceScore);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsRemote);

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            entity.Property(e => e.MatchedSkills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);

            entity.Property(e => e.MissingSkills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);

            var applyLinkComparer = new ValueComparer<List<ApplyLink>>(
                (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
                c => JsonSerializer.Deserialize<List<ApplyLink>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

            entity.Property(e => e.ApplyLinks)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v) ? new List<ApplyLink>() : JsonSerializer.Deserialize<List<ApplyLink>>(v, (JsonSerializerOptions?)null) ?? new List<ApplyLink>())
                .Metadata.SetValueComparer(applyLinkComparer);

            entity.Property(e => e.Status)
                .HasConversion<string>();
        });

        modelBuilder.Entity<MasterResume>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<TailoredDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.JobListing).WithMany().HasForeignKey(e => e.JobListingId);
            entity.HasOne(e => e.MasterResume).WithMany().HasForeignKey(e => e.MasterResumeId);
            entity.Property(e => e.LlmPrompt).HasColumnName("ClaudePrompt");
            entity.Property(e => e.LlmResponse).HasColumnName("ClaudeResponse");
        });

        modelBuilder.Entity<SearchProfile>(entity =>
        {
            entity.HasKey(e => e.Id);

            var profileStringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            entity.Property(e => e.RequiredSkills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(profileStringListComparer);

            entity.Property(e => e.PreferredSkills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(profileStringListComparer);

            entity.Property(e => e.TitleKeywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(profileStringListComparer);

            entity.Property(e => e.NegativeTitleKeywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(profileStringListComparer);
        });
    }
}
