namespace HomeRun.Models
{
    /// <summary>
    /// One of today's tasks, as shown by the Tidgets home screen widget.
    /// </summary>
    public class WidgetTask
    {
        /// <summary>
        /// The task name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the task is done today.
        /// </summary>
        public bool Done { get; set; }
    }
}