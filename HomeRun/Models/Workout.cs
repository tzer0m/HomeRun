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
        public string Summary => Type switch { WorkoutType.Strength => $"{Rounds} Rounds, {RestSeconds}s Rest", WorkoutType.Circuit => $"{Rounds} Rounds, {WorkSeconds}s On / {OffSeconds}s Off", _ => Description };

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