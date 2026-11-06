namespace HomeRun.Models
{
    /// <summary>
    /// The kind of workout.
    /// </summary>
    public enum WorkoutType
    {
        /// <summary>
        /// Rounds of moves with rest between rounds.
        /// </summary>
        Strength,
        /// <summary>
        /// Rounds of timed on and off intervals.
        /// </summary>
        Circuit,
        /// <summary>
        /// A checklist tick only, such as a walk or rest day.
        /// </summary>
        Tick,
        /// <summary>
        /// Rounds of timed stretch holds.
        /// </summary>
        Stretch
    }
}