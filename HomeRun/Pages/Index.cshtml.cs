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
    /// <param name="settingsService">The settings service.</param>
    public class IndexModel(PlanService planService, AppDbContext db, HistoryService history, SettingsService settingsService) : PageModel
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
        /// The display name of the other exercise item.
        /// </summary>
        public string OtherName { get; set; } = string.Empty;

        /// <summary>
        /// The other exercise entered today, or null if none.
        /// </summary>
        public string? OtherNote { get; set; }

        /// <summary>
        /// Whether an other exercise has been entered today.
        /// </summary>
        public bool OtherDone => !string.IsNullOrEmpty(OtherNote);

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
        /// Whether fasting is switched on.
        /// </summary>
        public bool FastingEnabled { get; set; }

        /// <summary>
        /// A suggestion to switch fasting off or back on based on the latest weigh-in, or empty if there is none.
        /// </summary>
        public string FastingPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Loads today's workout, stretching, other exercise, fasting state and whether weight is recorded.
        /// </summary>
        public async Task OnGetAsync()
        {
            LoadToday();
            WeightEntry? entry = await db.Weights.FirstOrDefaultAsync(x => x.Date == Today);
            Weight = entry?.Kilograms;
            FastingEnabled = (await settingsService.GetAsync()).FastingEnabled;
            decimal? latest = await db.Weights.OrderByDescending(x => x.Date).Select(x => (decimal?)x.Kilograms).FirstOrDefaultAsync();
            FastingPrompt = GetFastingPrompt(latest);
            Streak = HistoryService.GetStreak(await history.GetDaysAsync(Today));
            HashSet<string> done = (await db.Checklist.Where(x => x.Date == Today).Select(x => x.Item).ToListAsync()).ToHashSet();
            Items.Add(new ChecklistItem { Id = Stretching.Id, Name = Stretching.Name, Detail = $"{Stretching.Moves.Count} Moves", Done = done.Contains(Stretching.Id), HasSession = true });
            if (Workout != null && Workout.Type != WorkoutType.Rest)
            {
                Items.Add(new ChecklistItem { Id = Workout.Id, Name = Workout.Name, Detail = Workout.Summary, Done = done.Contains(Workout.Id), HasSession = Workout.Type != WorkoutType.Tick });
            }
            Workout other = planService.Plan.Other;
            OtherName = other.Name;
            OtherNote = await db.Checklist.Where(x => x.Date == Today && x.Item == other.Id).Select(x => x.Note).FirstOrDefaultAsync();
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
        /// Saves today's other exercise, replacing any existing entry, or removes it if the text is empty.
        /// </summary>
        /// <param name="note">What was done.</param>
        public async Task<IActionResult> OnPostOtherAsync(string? note)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            string otherId = planService.Plan.Other.Id;
            ChecklistEntry? entry = await db.Checklist.FirstOrDefaultAsync(x => x.Date == today && x.Item == otherId);
            if (string.IsNullOrWhiteSpace(note))
            {
                if (entry != null)
                {
                    db.Checklist.Remove(entry);
                }
            }
            else
            {
                if (entry == null)
                {
                    entry = new ChecklistEntry { Date = today, Item = otherId };
                    db.Checklist.Add(entry);
                }
                entry.Note = note.Trim();
            }
            await db.SaveChangesAsync();
            return RedirectToPage();
        }

        /// <summary>
        /// Switches fasting on or off.
        /// </summary>
        /// <param name="enabled">Whether fasting should be on.</param>
        public async Task<IActionResult> OnPostFastingAsync(bool enabled)
        {
            await settingsService.SetFastingAsync(enabled);
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

        /// <summary>
        /// Builds the prompt to switch fasting off when under the stop weight, or back on when over the resume weight.
        /// </summary>
        /// <param name="latest">The most recent weigh-in in kilograms, or null if there are none.</param>
        private string GetFastingPrompt(decimal? latest)
        {
            FastingWindow fasting = planService.Plan.Fasting;
            if (latest == null)
            {
                return string.Empty;
            }
            if (FastingEnabled && latest < fasting.StopBelowKg)
            {
                return $"Under {fasting.StopBelowKg:0.0} kg, you can switch fasting off";
            }
            if (!FastingEnabled && latest > fasting.ResumeAboveKg)
            {
                return $"Over {fasting.ResumeAboveKg:0.0} kg, time to fast again";
            }
            return string.Empty;
        }
    }
}