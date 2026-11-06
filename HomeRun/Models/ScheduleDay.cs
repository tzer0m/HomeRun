namespace HomeRun.Models
{
    /// <summary>
    /// The workout assigned to a day of the week.
    /// </summary>
    public class ScheduleDay
    {
        /// <summary>
        /// The day of the week.
        /// </summary>
        public DayOfWeek Day { get; set; }

        /// <summary>
        /// The ID of the workout, or null for a rest day.
        /// </summary>
        public string? WorkoutId { get; set; }
    }
}