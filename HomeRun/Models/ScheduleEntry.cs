namespace HomeRun.Models
{
    /// <summary>
    /// A day in a scheduled week with its progressed workout.
    /// </summary>
    public class ScheduleEntry
    {
        /// <summary>
        /// The day of the week.
        /// </summary>
        public DayOfWeek Day { get; set; }

        /// <summary>
        /// The workout with the week's progression applied, or null if nothing is scheduled.
        /// </summary>
        public Workout? Workout { get; set; }
    }
}