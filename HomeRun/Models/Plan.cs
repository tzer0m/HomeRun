namespace HomeRun.Models
{
    /// <summary>
    /// The full workout plan loaded from plan.json.
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// The date week 1 begins.
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// The daily eating window.
        /// </summary>
        public FastingWindow Fasting { get; set; } = new FastingWindow();

        /// <summary>
        /// The daily stretching routine.
        /// </summary>
        public Workout Stretching { get; set; } = new Workout();

        /// <summary>
        /// All available workouts.
        /// </summary>
        public List<Workout> Workouts { get; set; } = [];

        /// <summary>
        /// The workout for each day of the week.
        /// </summary>
        public List<ScheduleDay> Schedule { get; set; } = [];

        /// <summary>
        /// The progression for each week.
        /// </summary>
        public List<WeekProgression> Progression { get; set; } = [];
    }
}