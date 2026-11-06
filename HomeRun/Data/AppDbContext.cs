using HomeRun.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeRun.Data
{
    /// <summary>
    /// Database context for HomeRun.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        /// <summary>
        /// The recorded weights, one per day.
        /// </summary>
        public DbSet<WeightEntry> Weights { get; set; }

        /// <summary>
        /// Configures the model.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeightEntry>().HasIndex(x => x.Date).IsUnique();
            modelBuilder.Entity<WeightEntry>().Property(x => x.Kilograms).HasPrecision(5, 1);
        }
    }
}