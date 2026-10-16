using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Database;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Roles ----------
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.RoleID);
            entity.Property(r => r.RoleName).HasMaxLength(20).IsRequired();
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // ---------- Users ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserID);
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.ProfilePictureURL).HasMaxLength(500);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", t => t.HasCheckConstraint(
                "CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));
            entity.HasKey(e => e.EventID);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(150).IsRequired();
            entity.Property(e => e.DistanceKm).HasColumnType("decimal(5,2)");
            entity.Property(e => e.EventType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.BannerImageURL).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.EventsOrganised)
                  .HasForeignKey(e => e.OrganiserID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryID);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)");
            entity.Property(c => c.MaxParticipants).HasDefaultValue(100);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(en => en.EnrolmentID);
            entity.Property(en => en.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Confirmed");
            entity.Property(en => en.EnrolmentDate).HasDefaultValueSql("GETDATE()");

            entity.HasOne(en => en.User)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.UserID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryID)
                  .OnDelete(DeleteBehavior.Restrict);

            // Matches UQ_Enrolments_User_Category in the Part 1 script:
            // a participant may only enrol once per category.
            entity.HasIndex(en => new { en.UserID, en.CategoryID }).IsUnique();
        });

        // ---------- Results ----------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultID);
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Finished");

            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentID)
                  .OnDelete(DeleteBehavior.Cascade);

            // Matches EnrolmentID UNIQUE in the Part 1 SQL script (1-to-1 with Enrolments).
            entity.HasIndex(r => r.EnrolmentID).IsUnique();
        });

        // ---------- Seed Roles ----------
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = Role.Organiser },
            new Role { RoleID = 2, RoleName = Role.Participant }
        );
    }
}