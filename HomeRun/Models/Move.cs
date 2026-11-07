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
        /// Moves next to each other with the same block name are run together as one block, repeated for each set and side.
        /// </summary>
        public string Block { get; set; } = string.Empty;

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
            return new Move { Name = Name, Description = Description, Type = Type, Sets = Sets, Reps = Reps + extraReps, RepsMax = RepsMax + extraReps, Seconds = Seconds, SecondsMax = SecondsMax, Each = Each, Block = Block, LowerSeconds = Type == MoveType.Reps ? progression.LowerSeconds : 0 };
        }

        /// <summary>
        /// Builds the session steps for a block of moves, running the whole block for each side in turn.
        /// </summary>
        /// <param name="block">The moves in the block.</param>
        /// <param name="round">The round text, or empty if there are no rounds.</param>
        public static List<SessionStep> ToBlockSteps(List<Move> block, string round)
        {
            if (block.Count == 1 && block[0].Block.Length == 0)
            {
                return block[0].ToSteps(round);
            }
            List<SessionStep> steps = [];
            Move first = block[0];
            string[] sides = first.Each == null ? [string.Empty] : ["Left", "Right"];
            int sets = Math.Max(first.Sets, 1);
            foreach (string side in sides)
            {
                for (int set = 1; set <= sets; set++)
                {
                    string progress = SetText(round, set, sets);
                    foreach (Move move in block)
                    {
                        steps.Add(move.ToStep(move.Type == MoveType.Reps ? string.Empty : side, progress));
                    }
                }
            }
            return steps;
        }

        /// <summary>
        /// Builds the session steps for this move.
        /// </summary>
        /// <param name="round">The round text, or empty if there are no rounds.</param>
        public List<SessionStep> ToSteps(string round)
        {
            List<SessionStep> steps = [];
            string[] sides = Each == null ? [string.Empty] : ["Left", "Right"];
            int sets = Math.Max(Sets, 1);
            for (int set = 1; set <= sets; set++)
            {
                string progress = SetText(round, set, sets);
                if (Type == MoveType.Reps)
                {
                    steps.Add(ToStep(string.Empty, progress));
                    continue;
                }
                foreach (string side in sides)
                {
                    steps.Add(ToStep(side, progress));
                }
            }
            return steps;
        }

        /// <summary>
        /// Builds a single step for this move.
        /// </summary>
        /// <param name="side">Left or Right, or empty if not per side.</param>
        /// <param name="progress">The round and set text.</param>
        private SessionStep ToStep(string side, string progress)
        {
            if (Type == MoveType.Reps)
            {
                string lowering = LowerSeconds > 0 ? $", {LowerSeconds}s Lowering" : string.Empty;
                return new SessionStep { Kind = StepKind.Work, Title = Name, Detail = $"{Range(Reps, RepsMax)} Reps{EachText}{lowering}", Description = Description, Round = progress };
            }
            string title = side.Length == 0 ? Name : $"{Name} - {side} {Each}";
            return new SessionStep { Kind = StepKind.Work, Title = title, Detail = $"{Seconds}s", Description = Description, Seconds = Seconds, Round = progress };
        }

        /// <summary>
        /// Combines the round text with a set counter.
        /// </summary>
        /// <param name="round">The round text.</param>
        /// <param name="set">The current set.</param>
        /// <param name="sets">The total sets.</param>
        private static string SetText(string round, int set, int sets)
        {
            string setText = sets > 1 ? $"Set {set} of {sets}" : string.Empty;
            return string.Join(" · ", new[] { round, setText }.Where(x => x.Length > 0));
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