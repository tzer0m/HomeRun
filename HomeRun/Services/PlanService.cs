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
        /// Returns the current week number, clamped to the weeks in the progression.
        /// </summary>
        /// <param name="today">Today's date.</param>
        public int GetCurrentWeek(DateOnly today)
        {
            int week = ((today.DayNumber - Data.StartDate.DayNumber) / 7) + 1;
            return Math.Clamp(week, 1, Data.Progression.Count);
        }

        /// <summary>
        /// Builds every week of the schedule with its progression applied.
        /// </summary>
        public List<ScheduleWeek> GetSchedule()
        {
            List<ScheduleWeek> weeks = [];
            foreach (WeekProgression progression in Data.Progression.OrderBy(x => x.Week))
            {
                ScheduleWeek week = new ScheduleWeek { Number = progression.Week };
                foreach (ScheduleDay day in Data.Schedule.OrderBy(x => ((int)x.Day + 6) % 7))
                {
                    Workout? workout = Data.Workouts.FirstOrDefault(x => x.Id == day.WorkoutId);
                    week.Days.Add(new ScheduleEntry { Day = day.Day, Workout = workout?.Progress(progression) });
                }
                weeks.Add(week);
            }
            return weeks;
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