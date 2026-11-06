using HomeRun.Models;
using HomeRun.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeRun.Pages
{
    /// <summary>
    /// Page model for the schedule page.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    public class ScheduleModel(PlanService planService) : PageModel
    {
        /// <summary>
        /// The schedule weeks with progression applied.
        /// </summary>
        public List<ScheduleWeek> Weeks { get; set; } = [];

        /// <summary>
        /// The daily stretching routine.
        /// </summary>
        public Workout Stretching { get; set; } = new Workout();

        /// <summary>
        /// The current week number.
        /// </summary>
        public int CurrentWeek { get; set; }

        /// <summary>
        /// Loads the schedule and works out the current week.
        /// </summary>
        public void OnGet()
        {
            Weeks = planService.GetSchedule();
            Stretching = planService.Plan.Stretching;
            CurrentWeek = planService.GetCurrentWeek(DateOnly.FromDateTime(DateTime.Now));
        }
    }
}