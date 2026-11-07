namespace HomeRun.Models
{
    /// <summary>
    /// What the widget endpoint returns.
    /// </summary>
    public class WidgetResponse
    {
        /// <summary>
        /// Today's tasks: the weigh-in, stretching and the scheduled workout, if any.
        /// </summary>
        public List<WidgetTask> Tasks { get; set; } = [];
    }
}