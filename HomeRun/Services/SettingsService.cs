using HomeRun.Data;
using HomeRun.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeRun.Services
{
    /// <summary>
    /// Reads and saves the site's single row of settings.
    /// </summary>
    /// <param name="db">The database context.</param>
    public class SettingsService(AppDbContext db)
    {
        /// <summary>
        /// Returns the saved settings, or the defaults if nothing has been saved yet.
        /// </summary>
        public async Task<Settings> GetAsync()
        {
            return await db.Settings.FirstOrDefaultAsync() ?? new Settings();
        }

        /// <summary>
        /// Switches fasting on or off.
        /// </summary>
        /// <param name="enabled">Whether fasting should be on.</param>
        public async Task SetFastingAsync(bool enabled)
        {
            Settings? settings = await db.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Settings();
                db.Settings.Add(settings);
            }
            settings.FastingEnabled = enabled;
            await db.SaveChangesAsync();
        }
    }
}