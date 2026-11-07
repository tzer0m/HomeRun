namespace HomeRun.Models
{
    /// <summary>
    /// A single step in a session, played by the runner.
    /// </summary>
    public class SessionStep
    {
        /// <summary>
        /// Whether this is a move, a rest or a get ready countdown.
        /// </summary>
        public StepKind Kind { get; set; }

        /// <summary>
        /// The title shown and announced.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The reps or time shown and announced.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// How to do the move.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The countdown length in seconds, or 0 to wait for the Done button.
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// The round and set text, such as Round 2 of 3.
        /// </summary>
        public string Round { get; set; } = string.Empty;
    }
}