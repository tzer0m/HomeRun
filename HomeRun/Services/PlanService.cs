using HomeRun.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using IOPath = System.IO.Path;

namespace HomeRun.Services
{
    /// <summary>
    /// Loads the workout plan and builds the progressed schedule.
    /// </summary>
    /// <param name="options">The plan options.</param>
    public class PlanService(IOptions<PlanOptions> options)
    {
        /// <summary>
        /// The JSON options used to read plan.json.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

        /// <summary>
        /// The loaded plan.
        /// </summary>
        private readonly Plan Data = Load(options.Value);

        /// <summary>
        /// The loaded plan.
        /// </summary>
        public Plan Plan => Data;

        /// <summary>
        /// Returns the current week number, 0 before the start date, and no higher than the last week in the progression.
        /// </summary>
        /// <param name="today">Today's date.</param>
        public int GetCurrentWeek(DateOnly today)
        {
            int days = today.DayNumber - Data.StartDate.DayNumber;
            return days < 0 ? 0 : Math.Min((days / 7) + 1, Data.Progression.Max(x => x.Week));
        }

        /// <summary>
        /// Returns the progressed workout scheduled for a date, using the week's own schedule if it has one, or null if nothing is scheduled.
        /// </summary>
        /// <param name="date">The date to look up.</param>
        public Workout? GetWorkout(DateOnly date)
        {
            WeekProgression progression = GetProgression(GetCurrentWeek(date));
            ScheduleDay? day = (progression.Schedule ?? Data.Schedule).FirstOrDefault(x => x.Day == date.DayOfWeek);
            Workout? workout = Data.Workouts.FirstOrDefault(x => x.Id == day?.WorkoutId);
            return workout?.Progress(progression);
        }

        /// <summary>
        /// Returns a workout by ID with the current week's progression applied, or the stretching routine, or null if not found.
        /// </summary>
        /// <param name="id">The workout or stretching ID.</param>
        /// <param name="date">The date used to work out the week.</param>
        public Workout? GetWorkoutById(string id, DateOnly date)
        {
            if (id == Data.Stretching.Id)
            {
                return Data.Stretching;
            }
            WeekProgression progression = GetProgression(GetCurrentWeek(date));
            return Data.Workouts.FirstOrDefault(x => x.Id == id)?.Progress(progression);
        }

        /// <summary>
        /// Builds every week of the schedule from week 1 with its progression applied.
        /// </summary>
        public List<ScheduleWeek> GetSchedule()
        {
            List<ScheduleWeek> weeks = [];
            foreach (WeekProgression progression in Data.Progression.Where(x => x.Week > 0).OrderBy(x => x.Week))
            {
                ScheduleWeek week = new ScheduleWeek { Number = progression.Week };
                foreach (ScheduleDay day in (progression.Schedule ?? Data.Schedule).OrderBy(x => ((int)x.Day + 6) % 7))
                {
                    Workout? workout = Data.Workouts.FirstOrDefault(x => x.Id == day.WorkoutId);
                    week.Days.Add(new ScheduleEntry { Day = day.Day, Workout = workout?.Progress(progression) });
                }
                weeks.Add(week);
            }
            return weeks;
        }

        /// <summary>
        /// Returns the progression for a week, or week 1's if the week is not in the plan.
        /// </summary>
        /// <param name="week">The week number.</param>
        private WeekProgression GetProgression(int week)
        {
            return Data.Progression.FirstOrDefault(x => x.Week == week) ?? Data.Progression.First(x => x.Week == 1);
        }

        /// <summary>
        /// Reads and deserializes plan.json.
        /// </summary>
        /// <param name="planOptions">The plan options.</param>
        private static Plan Load(PlanOptions planOptions)
        {
            string path = IOPath.Combine(AppContext.BaseDirectory, planOptions.Path);
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Plan>(json, JsonOptions) ?? throw new InvalidOperationException($"Could not read the plan from {path}.");
        }
    }
}