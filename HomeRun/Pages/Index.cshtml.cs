using HomeRun.Data;
using HomeRun.Models;
using HomeRun.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HomeRun.Pages
{
    /// <summary>
    /// Page model for the home page, showing today's checklist.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="db">The database context.</param>
    /// <param name="history">The history service.</param>
    public class IndexModel(PlanService planService, AppDbContext db, HistoryService history) : PageModel
    {
        /// <summary>
        /// Today's date.
        /// </summary>
        public DateOnly Today { get; set; }

        /// <summary>
        /// The current week number.
        /// </summary>
        public int Week { get; set; }

        /// <summary>
        /// Today's workout with progression applied, or null if nothing is scheduled.
        /// </summary>
        public Workout? Workout { get; set; }

        /// <summary>
        /// The daily stretching routine.
        /// </summary>
        public Workout Stretching { get; set; } = new Workout();

        /// <summary>
        /// Today's recorded weight in kilograms, or null if not recorded yet.
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Whether today's weight has already been recorded.
        /// </summary>
        public bool WeightRecorded => Weight.HasValue;

        /// <summary>
        /// Today's checklist rows after weighing in.
        /// </summary>
        public List<ChecklistItem> Items { get; set; } = [];

        /// <summary>
        /// The current streak of complete days.
        /// </summary>
        public int Streak { get; set; }

        /// <summary>
        /// Whether the eating window is currently open.
        /// </summary>
        public bool Eating { get; set; }

        /// <summary>
        /// The moment the eating window next opens or closes.
        /// </summary>
        public DateTimeOffset FastingTarget { get; set; }

        /// <summary>
        /// The time remaining until the fasting target, for display.
        /// </summary>
        public string RemainingText { get; set; } = string.Empty;

        /// <summary>
        /// Loads today's workout, stretching, fasting state and whether weight is recorded.
        /// </summary>
        public async Task OnGetAsync()
        {
            LoadToday();
            WeightEntry? entry = await db.Weights.FirstOrDefaultAsync(x => x.Date == Today);
            Weight = entry?.Kilograms;
            Streak = HistoryService.GetStreak(await history.GetDaysAsync(Today));
            HashSet<string> done = (await db.Checklist.Where(x => x.Date == Today).Select(x => x.Item).ToListAsync()).ToHashSet();
            Items.Add(new ChecklistItem { Id = Stretching.Id, Name = Stretching.Name, Done = done.Contains(Stretching.Id), HasSession = true });
            if (Workout != null && Workout.Type != WorkoutType.Rest)
            {
                Items.Add(new ChecklistItem { Id = Workout.Id, Name = Workout.Name, Done = done.Contains(Workout.Id), HasSession = Workout.Type != WorkoutType.Tick });
            }
        }

        /// <summary>
        /// Saves today's weight, replacing any existing entry.
        /// </summary>
        /// <param name="weight">The weight in kilograms.</param>
        public async Task<IActionResult> OnPostAsync(decimal? weight)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            if (weight is > 0)
            {
                WeightEntry? entry = await db.Weights.FirstOrDefaultAsync(x => x.Date == today);
                if (entry == null)
                {
                    entry = new WeightEntry { Date = today };
                    db.Weights.Add(entry);
                }
                entry.Kilograms = weight.Value;
                await db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Ticks an item off for today, or unticks it if already ticked.
        /// </summary>
        /// <param name="item">The ID of the stretching routine or today's workout.</param>
        public async Task<IActionResult> OnPostTickAsync(string item)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            string? workoutId = planService.GetWorkout(today)?.Id;
            if (item == planService.Plan.Stretching.Id || item == workoutId)
            {
                ChecklistEntry? entry = await db.Checklist.FirstOrDefaultAsync(x => x.Date == today && x.Item == item);
                if (entry == null)
                {
                    db.Checklist.Add(new ChecklistEntry { Date = today, Item = item });
                }
                else
                {
                    db.Checklist.Remove(entry);
                }
                await db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Loads today's workout, stretching and fasting state.
        /// </summary>
        private void LoadToday()
        {
            DateTime now = DateTime.Now;
            Today = DateOnly.FromDateTime(now);
            Week = planService.GetCurrentWeek(Today);
            Workout = planService.GetWorkout(Today);
            Stretching = planService.Plan.Stretching;
            FastingWindow fasting = planService.Plan.Fasting;
            TimeOnly time = TimeOnly.FromDateTime(now);
            Eating = time >= fasting.Start && time < fasting.End;
            DateTime target = Eating ? now.Date.Add(fasting.End.ToTimeSpan()) : (time < fasting.Start ? now.Date.Add(fasting.Start.ToTimeSpan()) : now.Date.AddDays(1).Add(fasting.Start.ToTimeSpan()));
            FastingTarget = new DateTimeOffset(target);
            RemainingText = (target - now).ToString(@"hh\:mm\:ss");
        }
    }
}