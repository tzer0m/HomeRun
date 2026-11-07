namespace HomeRun.Models
{
    /// <summary>
    /// The kind of step in a session.
    /// </summary>
    public enum StepKind
    {
        /// <summary>
        /// A move to perform.
        /// </summary>
        Work,
        /// <summary>
        /// A rest between moves or rounds.
        /// </summary>
        Rest,
        /// <summary>
        /// A short countdown before a move starts.
        /// </summary>
        Ready
    }
}