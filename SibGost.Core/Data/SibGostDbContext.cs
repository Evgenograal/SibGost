using Microsoft.EntityFrameworkCore;
using SibGost.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;  // ✅ Добавьте это

namespace SibGost.Core.Data
{
    public class SibGostDbContext : DbContext
    {
        public SibGostDbContext(DbContextOptions<SibGostDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<AppRole> Roles => Set<AppRole>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PricePolicy> PricePolicies => Set<PricePolicy>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomAmenity>()
                .HasKey(ra => new { ra.RoomId, ra.AmenityId });

            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();
            modelBuilder.Entity<Room>().HasIndex(r => r.RoomNumber).IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role).WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User).WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room).WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);

        }
    }
}