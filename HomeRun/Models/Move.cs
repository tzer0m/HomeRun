namespace HomeRun.Models
{
    /// <summary>
    /// A single exercise within a workout.
    /// </summary>
    public class Move
    {
        /// <summary>
        /// The name of the move.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A short description of how to do the move.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the move is rep based, held or interval based.
        /// </summary>
        public MoveType Type { get; set; }

        /// <summary>
        /// The number of times the move is repeated back to back.
        /// </summary>
        public int Sets { get; set; } = 1;

        /// <summary>
        /// The reps, or the minimum reps when a range is given.
        /// </summary>
        public int Reps { get; set; }

        /// <summary>
        /// The maximum reps when a range is given.
        /// </summary>
        public int? RepsMax { get; set; }

        /// <summary>
        /// The hold seconds, or the minimum seconds when a range is given.
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// The maximum hold seconds when a range is given.
        /// </summary>
        public int? SecondsMax { get; set; }

        /// <summary>
        /// The label for moves done on each leg or side, such as Leg or Side.
        /// </summary>
        public string? Each { get; set; }

        /// <summary>
        /// The slow lowering time in seconds, or 0 for normal tempo.
        /// </summary>
        public int LowerSeconds { get; set; }

        /// <summary>
        /// The reps or hold time for display, including any slow lowering.
        /// </summary>
        public string Amount
        {
            get
            {
                string amount = Type switch { MoveType.Reps => Range(Reps, RepsMax) + EachText, MoveType.Timed => $"{Range(Seconds, SecondsMax)}s" + EachText, _ => string.Empty };
                if (Sets > 1 && amount.Length > 0)
                {
                    amount = $"{Sets} × {amount}";
                }
                return LowerSeconds > 0 ? $"{amount}, {LowerSeconds}s Lowering" : amount;
            }
        }

        /// <summary>
        /// The each leg or side label for display, or empty if not per side.
        /// </summary>
        private string EachText => Each == null ? string.Empty : $" Each {Each}";

        /// <summary>
        /// Returns a copy of this move with a week's progression applied.
        /// </summary>
        /// <param name="progression">The progression to apply.</param>
        public Move Progress(WeekProgression progression)
        {
            int extraReps = Type == MoveType.Reps ? progression.ExtraReps : 0;
            return new Move { Name = Name, Description = Description, Type = Type, Sets = Sets, Reps = Reps + extraReps, RepsMax = RepsMax + extraReps, Seconds = Seconds, SecondsMax = SecondsMax, Each = Each, LowerSeconds = Type == MoveType.Reps ? progression.LowerSeconds : 0 };
        }

        /// <summary>
        /// Formats a minimum and optional maximum as a range.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value, if any.</param>
        private static string Range(int min, int? max)
        {
            return max.HasValue ? $"{min}–{max}" : $"{min}";
        }
    }
}