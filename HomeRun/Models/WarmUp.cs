namespace HomeRun.Models
{
    /// <summary>
    /// The warm-up done before every session.
    /// </summary>
    public class WarmUp
    {
        /// <summary>
        /// The warm-up length in minutes.
        /// </summary>
        public int Minutes { get; set; }

        /// <summary>
        /// The names of the warm-up moves.
        /// </summary>
        public List<string> Moves { get; set; } = [];
    }
}