using HomeRun.Models;
using HomeRun.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeRun.Pages
{
    /// <summary>
    /// Page model for the home page, showing today's checklist.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    public class IndexModel(PlanService planService) : PageModel
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
        /// Whether today's weight has already been recorded.
        /// </summary>
        public bool WeightRecorded { get; set; }

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
        /// Loads today's workout, stretching and fasting state.
        /// </summary>
        public void OnGet()
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