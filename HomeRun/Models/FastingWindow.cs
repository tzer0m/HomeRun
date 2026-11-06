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
    }
}