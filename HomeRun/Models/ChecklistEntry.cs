namespace HomeRun.Models
{
    /// <summary>
    /// A checklist item ticked off on a given day.
    /// </summary>
    public class ChecklistEntry
    {
        /// <summary>
        /// The primary key.
        /// </summary>
        public int ChecklistEntryId { get; set; }

        /// <summary>
        /// The date the item was completed.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// The ID of the completed workout or routine.
        /// </summary>
        public string Item { get; set; } = string.Empty;
    }
}