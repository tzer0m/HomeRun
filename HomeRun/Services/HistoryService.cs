using HomeRun.Data;
using HomeRun.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeRun.Services
{
    /// <summary>
    /// Builds the day by day history and streaks from the recorded weights and ticks.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="db">The database context.</param>
    public class HistoryService(PlanService planService, AppDbContext db)
    {
        /// <summary>
        /// Returns every day from the first record up to today, oldest first.
        /// </summary>
        /// <param name="today">Today's date.</param>
        public async Task<List<HistoryDay>> GetDaysAsync(DateOnly today)
        {
            List<WeightEntry> weights = await db.Weights.ToListAsync();
            List<ChecklistEntry> entries = await db.Checklist.ToListAsync();
            List<DateOnly> dates = weights.Select(x => x.Date).Concat(entries.Select(x => x.Date)).ToList();
            List<HistoryDay> days = [];
            if (dates.Count == 0)
            {
                return days;
            }
            string stretchingId = planService.Plan.Stretching.Id;
            string padelId = planService.Plan.Padel.Id;
            for (DateOnly date = dates.Min(); date <= today; date = date.AddDays(1))
            {
                Workout? workout = planService.GetWorkout(date);
                if (workout?.Type == WorkoutType.Rest)
                {
                    workout = null;
                }
                HashSet<string> done = entries.Where(x => x.Date == date).Select(x => x.Item).ToHashSet();
                decimal? kilograms = weights.FirstOrDefault(x => x.Date == date)?.Kilograms;
                days.Add(new HistoryDay { Date = date, Kilograms = kilograms, Stretching = done.Contains(stretchingId), WorkoutName = workout?.Name ?? string.Empty, WorkoutDone = workout != null && done.Contains(workout.Id), Padel = done.Contains(padelId) });
            }
            return days;
        }

        /// <summary>
        /// Counts the complete days in a row up to today, where an unfinished today does not break the streak.
        /// </summary>
        /// <param name="days">The days, oldest first.</param>
        public static int GetStreak(List<HistoryDay> days)
        {
            int streak = 0;
            for (int i = days.Count - 1; i >= 0; i--)
            {
                if (days[i].Complete)
                {
                    streak++;
                }
                else if (i != days.Count - 1)
                {
                    break;
                }
            }
            return streak;
        }

        /// <summary>
        /// Finds the longest run of complete days.
        /// </summary>
        /// <param name="days">The days, oldest first.</param>
        public static int GetBestStreak(List<HistoryDay> days)
        {
            int best = 0;
            int run = 0;
            foreach (HistoryDay day in days)
            {
                run = day.Complete ? run + 1 : 0;
                best = Math.Max(best, run);
            }
            return best;
        }
    }
}