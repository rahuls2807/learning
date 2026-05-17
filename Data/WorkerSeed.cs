using Microsoft.EntityFrameworkCore;
using WorkerBookingSystem.Models;
using System;

namespace WorkerBookingSystem.Data
{
    public static class WorkerSeed
    {
        public static void SeedData(WorkerBookingContext context)
        {
            if (context.HourlyRates.Any()) return;

            // Sample Workers
            if (!context.Workers.Any())
            {
                var workers = new Worker[]
                {
                    new Worker { FirstName = "John", LastName = "Doe", Email = "john@worker.com", PhoneNumber = "123-456", Skill = "Plumbing", IsActive = true },
                    new Worker { FirstName = "Jane", LastName = "Smith", Email = "jane@worker.com", PhoneNumber = "789-012", Skill = "Electrical", IsActive = true },
                    new Worker { FirstName = "Bob", LastName = "Johnson", Email = "bob@worker.com", PhoneNumber = "345-678", Skill = "Carpentry", IsActive = true }
                };

                context.Workers.AddRange(workers);
                context.SaveChanges();
            }

            // Sample Clients
            if (!context.Clients.Any())
            {
                var clients = new Client[]
                {
                    new Client { FirstName = "Alice", LastName = "Brown", Email = "alice@client.com", PhoneNumber = "111-222", Address = "123 Main St" },
                    new Client { FirstName = "Charlie", LastName = "Davis", Email = "charlie@client.com", PhoneNumber = "333-444", Address = "456 Oak Ave" }
                };

                context.Clients.AddRange(clients);
                context.SaveChanges();
            }

            // Sample HourlyRates
            var rates = new HourlyRate[]
            {
                new HourlyRate { WorkerId = 1, Skill = "Plumbing", RatePerHour = 25.00m, IsActive = true },
                new HourlyRate { WorkerId = 2, Skill = "Electrical", RatePerHour = 35.00m, IsActive = true },
                new HourlyRate { WorkerId = 1, Skill = "Plumbing", RatePerHour = 25.00m, IsActive = true },
                new HourlyRate { WorkerId = 2, Skill = "Electrical", RatePerHour = 35.00m, IsActive = true },
                new HourlyRate { WorkerId = 3, Skill = "Carpentry", RatePerHour = 30.00m, IsActive = true }
            };


            context.HourlyRates.AddRange(rates);
            context.SaveChanges();

            // Sample Availabilities
            var availabilities = new WorkerAvailability[]
            {
                new WorkerAvailability { WorkerId = 1, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9,0,0), EndTime = new TimeSpan(17,0,0), IsAvailable = true },
                new WorkerAvailability { WorkerId = 2, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(10,0,0), EndTime = new TimeSpan(18,0,0), IsAvailable = true }
            };




            context.WorkerAvailabilities.AddRange(availabilities);
            context.SaveChanges();

            // Sample Booking
            var bookingDate = DateTime.Now.AddDays(1);
            var booking = new Booking 
            { 
                WorkerId = 1, 
                ClientId = 1, 
                BookingDate = bookingDate.Date, 
                StartTime = bookingDate.Date.AddHours(10), 
                EndTime = bookingDate.Date.AddHours(12), 
                TaskDescription = "Fix leak", 
                TotalWage = 50.00m, 
                Status = BookingStatus.Pending, 
                CreatedDate = DateTime.Now 
            };



            context.Bookings.Add(booking);
            context.SaveChanges();
        }
    }
}
