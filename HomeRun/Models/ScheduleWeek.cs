namespace HomeRun.Models
{
    /// <summary>
    /// A week of the schedule with progression applied.
    /// </summary>
    public class ScheduleWeek
    {
        /// <summary>
        /// The week number, starting at 1.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// The days of the week.
        /// </summary>
        public List<ScheduleEntry> Days { get; set; } = [];
    }
}