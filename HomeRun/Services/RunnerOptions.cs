namespace HomeRun.Services
{
    /// <summary>
    /// Settings for the session runner.
    /// </summary>
    public class RunnerOptions
    {
        /// <summary>
        /// The get ready countdown before a move, in seconds.
        /// </summary>
        public int TransitionSeconds { get; set; }
    }
}