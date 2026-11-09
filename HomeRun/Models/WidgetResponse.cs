namespace HomeRun.Models
{
    /// <summary>
    /// What the widget endpoint returns.
    /// </summary>
    public class WidgetResponse
    {
        /// <summary>
        /// Today's tasks: the weigh-in, stretching and an activity done by the workout or padel, if a workout is scheduled.
        /// </summary>
        public List<WidgetTask> Tasks { get; set; } = [];
    }
}