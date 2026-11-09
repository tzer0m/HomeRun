namespace HomeRun.Models
{
    /// <summary>
    /// What was recorded and completed on a single day.
    /// </summary>
    public class HistoryDay
    {
        /// <summary>
        /// The date.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// The weight recorded that day in kilograms, or null if none.
        /// </summary>
        public decimal? Kilograms { get; set; }

        /// <summary>
        /// Whether the stretching routine was ticked.
        /// </summary>
        public bool Stretching { get; set; }

        /// <summary>
        /// The name of the workout scheduled that day, or empty if none.
        /// </summary>
        public string WorkoutName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the scheduled workout was ticked.
        /// </summary>
        public bool WorkoutDone { get; set; }

        /// <summary>
        /// Whether padel was ticked, which counts as the day's workout.
        /// </summary>
        public bool Padel { get; set; }

        /// <summary>
        /// Whether stretching and either the scheduled workout or padel were ticked.
        /// </summary>
        public bool Complete => Stretching && (WorkoutName.Length == 0 || WorkoutDone || Padel);
    }
}