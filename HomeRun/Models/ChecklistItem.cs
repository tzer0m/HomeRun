namespace HomeRun.Models
{
    /// <summary>
    /// A row on today's checklist.
    /// </summary>
    public class ChecklistItem
    {
        /// <summary>
        /// The ID of the workout or routine.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether it has been ticked off today.
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// Whether it can be launched in the session runner.
        /// </summary>
        public bool HasSession { get; set; }
    }
}