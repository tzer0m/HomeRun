namespace HomeRun.Models
{
    /// <summary>
    /// The changes applied to every workout in a given week.
    /// </summary>
    public class WeekProgression
    {
        /// <summary>
        /// The week number, starting at 1.
        /// </summary>
        public int Week { get; set; }

        /// <summary>
        /// Extra reps added to rep based moves.
        /// </summary>
        public int ExtraReps { get; set; }

        /// <summary>
        /// The slow lowering time in seconds for rep based moves, or 0 for normal tempo.
        /// </summary>
        public int LowerSeconds { get; set; }

        /// <summary>
        /// Overrides the rest between rounds for strength workouts.
        /// </summary>
        public int? RestSeconds { get; set; }

        /// <summary>
        /// Overrides the seconds on for circuits.
        /// </summary>
        public int? WorkSeconds { get; set; }

        /// <summary>
        /// Overrides the seconds off for circuits.
        /// </summary>
        public int? OffSeconds { get; set; }
    }
}