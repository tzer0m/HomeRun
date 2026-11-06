namespace HomeRun.Models
{
    /// <summary>
    /// A weight recorded on a given day.
    /// </summary>
    public class WeightEntry
    {
        /// <summary>
        /// The primary key.
        /// </summary>
        public int WeightEntryId { get; set; }

        /// <summary>
        /// The date the weight was recorded.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// The weight in kilograms.
        /// </summary>
        public decimal Kilograms { get; set; }
    }
}