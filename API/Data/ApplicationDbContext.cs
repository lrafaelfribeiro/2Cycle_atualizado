using API.Data.Conversions;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<User> Users
        {
            get; set;
        }
        public DbSet<Profile> Profiles
        {
            get; set;
        }
        public DbSet<RefreshToken> RefreshTokens
        {
            get; set;
        }

        public DbSet<WeightLog> WeightLogs { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackSegment> TrackSegments { get; set; }
        public DbSet<TrackPoint> TrackPoints { get; set; }

        public DbSet<UserSuggestedRoute> UserSuggestedRoutes { get; set; }
        public DbSet<SuggestedRoute> SuggestedRoutes { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email_Unique");

                entity.Property(u => u.CreatedAt)
                      .HasPrecision(3);

            });

            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithOne(u => u.Profile)
                      .HasForeignKey<Profile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.UserId)
                      .IsUnique()
                      .HasDatabaseName("IX_UserProfiles_UserId_Unique");

                entity.Property(up => up.CreatedAt)
                      .HasPrecision(3);

                entity.Property(up => up.LastUpdatedAt)
                      .HasPrecision(3);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(rt => rt.HashToken)
                      .IsUnique()
                      .HasDatabaseName("IX_RefreshTokens_HashToken_Unique");

                entity.Property(rt => rt.CreatedAt)
                      .HasPrecision(3);

                entity.Property(rt => rt.ExpiresAt)
                      .HasPrecision(3);
            });

            modelBuilder.Entity<WeightLog>()
                    .HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WeightLog>()
                .HasIndex(w => new { w.UserId, w.RecordedAt });

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Activity>()
                .HasIndex(a => new { a.UserId, a.StartedAt });

            // Tracks

            modelBuilder.Entity<Track>()
                .HasOne(t => t.Activity)
                .WithOne(a => a.Track)
                .HasForeignKey<Track>(t => t.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackSegment>()
                .HasOne(s => s.Track)
                .WithMany(t => t.Segments)
                .HasForeignKey(s => s.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackPoint>()
                .HasOne(p => p.TrackSegment)
                .WithMany(s => s.Points)
                .HasForeignKey(p => p.TrackSegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackPoint>()
                .HasIndex(p => new { p.TrackSegmentId, p.Sequence });

            // Sugestao de Rotas
            modelBuilder.Entity<UserSuggestedRoute>()
                 .HasOne(usr => usr.User)
                 .WithMany()
                 .HasForeignKey(usr => usr.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSuggestedRoute>()
                .HasOne(usr => usr.SuggestedRoute)
                .WithMany(r => r.SavedByUsers)
                .HasForeignKey(usr => usr.SuggestedRouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSuggestedRoute>()
                .HasIndex(usr => new { usr.UserId, usr.SuggestedRouteId })
                .IsUnique();

            modelBuilder.Entity<SuggestedRoute>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            modelBuilder.Entity<RoutePoint>()
                .HasOne(p => p.SuggestedRoute)
                .WithMany(r => r.Points)
                .HasForeignKey(p => p.SuggestedRouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoutePoint>()
                .HasIndex(p => new { p.SuggestedRouteId, p.Sequence });
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>();

            configurationBuilder.Properties<DateTime?>()
                .HaveConversion<NullableUtcDateTimeConverter>();
        }
    }
}


