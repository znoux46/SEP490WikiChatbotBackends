using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.API.Domain.Entities;

namespace WikiChatbotBackends.API.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<NotablePerson> NotablePersons { get; set; }
    public DbSet<Award> Awards { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PersonAward> PersonAwards { get; set; }
    public DbSet<PersonOrganization> PersonOrganizations { get; set; }
    public DbSet<PersonTag> PersonTags { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ChatHistory> ChatHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NotablePerson configuration
        modelBuilder.Entity<NotablePerson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Biography).HasColumnType("text");
            entity.Property(e => e.Nationality).HasMaxLength(100);
        });

        // Award configuration
        modelBuilder.Entity<Award>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("text");
        });

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("text");
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // PersonAward configuration
        modelBuilder.Entity<PersonAward>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Person)
                .WithMany(p => p.PersonAwards)
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Award)
                .WithMany(a => a.PersonAwards)
                .HasForeignKey(e => e.AwardId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.PersonId, e.AwardId }).IsUnique();
        });

        // PersonOrganization configuration
        modelBuilder.Entity<PersonOrganization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.HasOne(e => e.Person)
                .WithMany(p => p.PersonOrganizations)
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.PersonOrganizations)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PersonTag configuration
        modelBuilder.Entity<PersonTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Person)
                .WithMany(p => p.PersonTags)
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.PersonTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.PersonId, e.TagId }).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // ChatHistory configuration
        modelBuilder.Entity<ChatHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Question).IsRequired().HasColumnType("text");
            entity.Property(e => e.Answer).IsRequired().HasColumnType("text");
            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatHistories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.SessionId });
        });
    }
}
