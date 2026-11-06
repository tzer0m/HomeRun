namespace HomeRun.Models
{
    /// <summary>
    /// How a move is performed.
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// Performed for a number of reps, finished with the Done button.
        /// </summary>
        Reps,
        /// <summary>
        /// Held for a number of seconds.
        /// </summary>
        Timed,
        /// <summary>
        /// Performed for the workout's on and off interval timings.
        /// </summary>
        Interval
    }
}