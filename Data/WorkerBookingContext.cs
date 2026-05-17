using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Data
{
    public class WorkerBookingContext : IdentityDbContext<ApplicationUser>
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
        public DbSet<WorkerReview> WorkerReviews { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


// Seed data with HasData

            // Configure Worker entity
            modelBuilder.Entity<Worker>()
                .HasKey(w => w.WorkerId);

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.UserId);

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.Skill);

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.IsActive);

            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.PhoneNumber);

            modelBuilder.Entity<WorkerReview>()
                .HasKey(r => r.WorkerReviewId);

            modelBuilder.Entity<WorkerReview>()
                .HasIndex(r => new { r.WorkerId, r.CreatedDate });

            modelBuilder.Entity<WorkerReview>()
                .HasOne(r => r.Worker)
                .WithMany(w => w.Reviews)
                .HasForeignKey(r => r.WorkerId);

            modelBuilder.Entity<WorkerReview>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkerReview>()
                .HasOne(r => r.Booking)
                .WithMany()
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

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

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.UserId);

            // Configure Booking entity
            modelBuilder.Entity<Booking>()
                .HasKey(b => b.BookingId);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.ClientId, b.BookingDate });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.WorkerId, b.BookingDate });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.PaymentStatus);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.Status);

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
                .HasIndex(hr => new { hr.WorkerId, hr.IsActive, hr.EffectiveDate });

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

            modelBuilder.Entity<Booking>()
                .Property(b => b.AmountPaidOnline)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.AmountPaidToWorker)
                .HasPrecision(10, 2);

            // Seed sample data
            // modelBuilder.Entity<Worker>().HasData(
            //     new Worker { WorkerId = 1, FirstName = "John", LastName = "Doe", Email = "john@worker.com", PhoneNumber = "123-456", Skill = "Plumbing", IsActive = true, CreatedDate = DateTime.Parse("2024-1-1") },
            //     new Worker { WorkerId = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@worker.com", PhoneNumber = "789-012", Skill = "Electrical", IsActive = true, CreatedDate = DateTime.Parse("2024-1-1") }
            // );

            // modelBuilder.Entity<Client>().HasData(
            //     new Client { ClientId = 1, FirstName = "Alice", LastName = "Brown", Email = "alice@client.com", PhoneNumber = "111-222", Address = "123 Main St", IsActive = true, CreatedDate = DateTime.Parse("2024-1-1") }
            // );

            // modelBuilder.Entity<HourlyRate>().HasData(
            //     new HourlyRate { RateId = 1, WorkerId = 1, Skill = "Plumbing", RatePerHour = 25.00m, EffectiveDate = DateTime.Parse("2024-1-1"), IsActive = true },
            //     new HourlyRate { RateId = 2, WorkerId = 2, Skill = "Electrical", RatePerHour = 35.00m, EffectiveDate = DateTime.Parse("2024-1-1"), IsActive = true }
            // );

            // modelBuilder.Entity<Booking>().HasData(
            //     new Booking { BookingId = 1, WorkerId = 1, ClientId = 1, BookingDate = DateTime.Parse("2024-10-1"), StartTime = DateTime.Parse("2024-10-1 10:00"), EndTime = DateTime.Parse("2024-10-1 12:00"), TaskDescription = "Fix leak", TotalWage = 50.00m, Status = BookingStatus.Pending, CreatedDate = DateTime.Parse("2024-1-1") }
            // );
        }
    }
}
