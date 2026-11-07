namespace HomeRun.Models
{
    /// <summary>
    /// A named workout made up of moves.
    /// </summary>
    public class Workout
    {
        /// <summary>
        /// The unique ID used by the schedule.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The display name of the workout.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a strength workout, a circuit or a tick only.
        /// </summary>
        public WorkoutType Type { get; set; }

        /// <summary>
        /// A description, used for tick only workouts.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The number of rounds.
        /// </summary>
        public int Rounds { get; set; }

        /// <summary>
        /// The rest between rounds in seconds, for strength workouts.
        /// </summary>
        public int RestSeconds { get; set; }

        /// <summary>
        /// The seconds on per exercise, for circuits.
        /// </summary>
        public int WorkSeconds { get; set; }

        /// <summary>
        /// The seconds off per exercise, for circuits.
        /// </summary>
        public int OffSeconds { get; set; }

        /// <summary>
        /// The moves in the workout, in order.
        /// </summary>
        public List<Move> Moves { get; set; } = [];

        /// <summary>
        /// A short description of the rounds and timings.
        /// </summary>
        public string Summary => Type switch { WorkoutType.Strength => $"{Rounds} Rounds, {RestSeconds}s Rest", WorkoutType.Circuit => $"{Rounds} Rounds, {WorkSeconds}s On / {OffSeconds}s Off", WorkoutType.Stretch => string.Empty, _ => Description };

        /// <summary>
        /// Builds the steps for running this workout, including rests and get ready countdowns.
        /// </summary>
        /// <param name="transitionSeconds">The get ready countdown length in seconds.</param>
        public List<SessionStep> BuildSteps(int transitionSeconds)
        {
            List<SessionStep> steps = [];
            int rounds = Math.Max(Rounds, 1);
            for (int round = 1; round <= rounds; round++)
            {
                string roundText = Type == WorkoutType.Stretch || rounds == 1 ? string.Empty : $"Round {round} of {rounds}";
                int index = 0;
                while (index < Moves.Count)
                {
                    Move move = Moves[index];
                    if (Type == WorkoutType.Circuit)
                    {
                        steps.Add(new SessionStep { Kind = StepKind.Work, Title = move.Name, Detail = $"{WorkSeconds}s", Description = move.Description, Seconds = WorkSeconds, Round = roundText });
                        if (OffSeconds > 0)
                        {
                            steps.Add(RestStep(OffSeconds, roundText));
                        }
                        index++;
                        continue;
                    }
                    List<Move> block = move.Block.Length == 0 ? [move] : Moves.Skip(index).TakeWhile(x => x.Block == move.Block).ToList();
                    steps.AddRange(Move.ToBlockSteps(block, roundText));
                    index += block.Count;
                }
                if (Type == WorkoutType.Strength && RestSeconds > 0)
                {
                    steps.Add(RestStep(RestSeconds, roundText));
                }
            }
            while (steps.Count > 0 && steps[^1].Kind == StepKind.Rest)
            {
                steps.RemoveAt(steps.Count - 1);
            }
            List<SessionStep> result = [];
            foreach (SessionStep step in steps)
            {
                bool previousIsRest = result.Count > 0 && result[^1].Kind == StepKind.Rest;
                if (transitionSeconds > 0 && step.Kind == StepKind.Work && step.Seconds > 0 && !previousIsRest)
                {
                    result.Add(new SessionStep { Kind = StepKind.Ready, Title = "Get Ready", Detail = step.Detail, Seconds = transitionSeconds, Round = step.Round });
                }
                result.Add(step);
            }
            return result;
        }

        /// <summary>
        /// Creates a rest step.
        /// </summary>
        /// <param name="seconds">The rest length in seconds.</param>
        /// <param name="round">The round text.</param>
        private static SessionStep RestStep(int seconds, string round)
        {
            return new SessionStep { Kind = StepKind.Rest, Title = "Rest", Detail = $"{seconds}s", Seconds = seconds, Round = round };
        }

        /// <summary>
        /// Returns a copy of this workout with a week's progression applied.
        /// </summary>
        /// <param name="progression">The progression to apply.</param>
        public Workout Progress(WeekProgression progression)
        {
            bool strength = Type == WorkoutType.Strength;
            bool circuit = Type == WorkoutType.Circuit;
            return new Workout { Id = Id, Name = Name, Type = Type, Description = Description, Rounds = Rounds, RestSeconds = strength ? (progression.RestSeconds ?? RestSeconds) : RestSeconds, WorkSeconds = circuit ? (progression.WorkSeconds ?? WorkSeconds) : WorkSeconds, OffSeconds = circuit ? (progression.OffSeconds ?? OffSeconds) : OffSeconds, Moves = Moves.Select(x => x.Progress(progression)).ToList() };
        }
    }
}