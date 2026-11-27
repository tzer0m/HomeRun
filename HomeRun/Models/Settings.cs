namespace HomeRun.Models
{
    /// <summary>
    /// The site's saved settings, stored as a single row.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The primary key.
        /// </summary>
        public int SettingsId { get; set; }

        /// <summary>
        /// Whether fasting is switched on.
        /// </summary>
        public bool FastingEnabled { get; set; } = true;
    }
}