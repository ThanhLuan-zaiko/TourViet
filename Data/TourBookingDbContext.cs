using Microsoft.EntityFrameworkCore;
using TourViet.Models;

namespace TourViet.Data;

public class TourBookingDbContext(DbContextOptions<TourBookingDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TourInstance> TourInstances { get; set; }
    public DbSet<TourPrice> TourPrices { get; set; }
    public DbSet<Itinerary> Itineraries { get; set; }
    public DbSet<TourImage> TourImages { get; set; }
    public DbSet<TourService> TourServices { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Models.BookingService> BookingServices { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionRule> PromotionRules { get; set; }
    public DbSet<PromotionTarget> PromotionTargets { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<PromotionRedemption> PromotionRedemptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserID);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.PasswordSalt).HasMaxLength(64);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleID);
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserID, e.RoleID });
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleID)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Tour entity
        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.TourID);
            entity.HasIndex(e => e.TourName);
            entity.HasIndex(e => e.Slug);
            entity.HasOne(e => e.Location)
                .WithMany(l => l.Tours)
                .HasForeignKey(e => e.LocationID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Tours)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DefaultGuide)
                .WithMany()
                .HasForeignKey(e => e.DefaultGuideID)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.LocationID);
            entity.HasOne(e => e.Country)
                .WithMany(c => c.Locations)
                .HasForeignKey(e => e.CountryID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Latitude).HasPrecision(18, 6);
            entity.Property(e => e.Longitude).HasPrecision(18, 6);
        });
        
        // Configure Country entity
        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.CountryID);
            entity.HasIndex(e => e.CountryName).IsUnique();
            entity.HasIndex(e => e.ISO2).IsUnique();
            // ISO3 index removed - we don't have real ISO3 codes from reverse geocoding, allowing multiple NULLs
        });
        
        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryID);
            entity.HasIndex(e => e.CategoryName).IsUnique();
        });
        
        // Configure TourInstance entity
        modelBuilder.Entity<TourInstance>(entity =>
        {
            entity.HasKey(e => e.InstanceID);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.TourInstances)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Guide)
                .WithMany()
                .HasForeignKey(e => e.GuideID)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure TourPrice entity
        modelBuilder.Entity<TourPrice>(entity =>
        {
            entity.HasKey(e => e.TourPriceID);
            entity.HasOne(e => e.TourInstance)
                .WithMany(ti => ti.TourPrices)
                .HasForeignKey(e => e.InstanceID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.TourPrices)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Itinerary entity
        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.HasKey(e => e.ItineraryID);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.Itineraries)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure TourImage entity
        modelBuilder.Entity<TourImage>(entity =>
        {
            entity.HasKey(e => e.ImageID);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.TourImages)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedByUser)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure TourService entity
        modelBuilder.Entity<TourService>(entity =>
        {
            entity.HasKey(e => e.TourServiceID);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.TourServices)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Service)
                .WithMany(s => s.TourServices)
                .HasForeignKey(e => e.ServiceID)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Service entity
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceID);
            entity.HasIndex(e => e.ServiceName);
        });
        
        // Configure Review entity
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewID);
            entity.HasOne(e => e.Tour)
                .WithMany(t => t.Reviews)
                .HasForeignKey(e => e.TourID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        
        // Configure Booking entity
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingID);
            entity.HasIndex(e => e.BookingRef).IsUnique();
            entity.HasOne(e => e.TourInstance)
                .WithMany(ti => ti.Bookings)
                .HasForeignKey(e => e.InstanceID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure BookingService entity
        modelBuilder.Entity<Models.BookingService>(entity =>
        {
            entity.HasKey(e => e.BookingServiceID);
            entity.HasOne(e => e.Booking)
                .WithMany()
                .HasForeignKey(e => e.BookingID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentID);
            entity.HasOne(e => e.Booking)
                .WithMany()
                .HasForeignKey(e => e.BookingID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Promotion entity
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionID);
            entity.HasIndex(e => e.Slug);
            entity.HasIndex(e => new { e.IsActive, e.StartAt, e.EndAt });
        });

        // Configure PromotionRule entity
        modelBuilder.Entity<PromotionRule>(entity =>
        {
            entity.HasKey(e => e.RuleID);
            entity.HasOne(e => e.Promotion)
                .WithMany(p => p.PromotionRules)
                .HasForeignKey(e => e.PromotionID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PromotionTarget entity
        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.HasKey(e => e.PromotionTargetID);
            entity.HasOne(e => e.Promotion)
                .WithMany(p => p.PromotionTargets)
                .HasForeignKey(e => e.PromotionID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.TargetType, e.TargetID });
        });

        // Configure Coupon entity
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponID);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.Promotion)
                .WithMany(p => p.Coupons)
                .HasForeignKey(e => e.PromotionID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PromotionRedemption entity
        modelBuilder.Entity<PromotionRedemption>(entity =>
        {
            entity.HasKey(e => e.RedemptionID);
            entity.HasOne(e => e.Promotion)
                .WithMany(p => p.PromotionRedemptions)
                .HasForeignKey(e => e.PromotionID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Coupon)
                .WithMany(c => c.PromotionRedemptions)
                .HasForeignKey(e => e.CouponID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Booking)
                .WithMany()
                .HasForeignKey(e => e.BookingID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.PromotionID);
            entity.HasIndex(e => e.UserID);
        });
    }
}

