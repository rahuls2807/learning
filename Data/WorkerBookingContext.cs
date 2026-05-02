using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Data
{
    public class WorkerBookingContext : DbContext
    {
        public WorkerBookingContext(DbContextOptions<WorkerBookingContext> options) : base(options)
        {
        }

        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerAvailability> WorkerAvailabilities { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<HourlyRate> HourlyRates { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Worker entity
            modelBuilder.Entity<Worker>()
                .HasKey(w => w.WorkerId);

            // Configure WorkerAvailability entity
            modelBuilder.Entity<WorkerAvailability>()
                .HasKey(wa => wa.AvailabilityId);

            modelBuilder.Entity<WorkerAvailability>()
                .HasOne(wa => wa.Worker)
                .WithMany(w => w.Availabilities)
                .HasForeignKey(wa => wa.WorkerId);

            // Configure Client entity
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ClientId);

            // Configure Booking entity
            modelBuilder.Entity<Booking>()
                .HasKey(b => b.BookingId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Worker)
                .WithMany(w => w.Bookings)
                .HasForeignKey(b => b.WorkerId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Client)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.ClientId);

            // Configure HourlyRate entity
            modelBuilder.Entity<HourlyRate>()
                .HasKey(hr => hr.RateId);

            modelBuilder.Entity<HourlyRate>()
                .HasOne(hr => hr.Worker)
                .WithMany()
                .HasForeignKey(hr => hr.WorkerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Admin entity
            modelBuilder.Entity<Admin>()
                .HasKey(a => a.AdminId);

            // Configure decimal precision for wages
            modelBuilder.Entity<HourlyRate>()
                .Property(hr => hr.RatePerHour)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalWage)
                .HasPrecision(10, 2);
        }
    }
}
