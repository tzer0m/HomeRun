namespace HomeRun.Models
{
    /// <summary>
    /// The daily window during which eating is allowed.
    /// </summary>
    public class FastingWindow
    {
        /// <summary>
        /// The time the eating window opens.
        /// </summary>
        public TimeOnly Start { get; set; }

        /// <summary>
        /// The time the eating window closes.
        /// </summary>
        public TimeOnly End { get; set; }

        /// <summary>
        /// The weight in kilograms below which the home page suggests switching fasting off, or null for no prompt.
        /// </summary>
        public decimal? StopBelowKg { get; set; }

        /// <summary>
        /// The weight in kilograms above which the home page suggests switching fasting back on, or null for no prompt.
        /// </summary>
        public decimal? ResumeAboveKg { get; set; }
    }
}