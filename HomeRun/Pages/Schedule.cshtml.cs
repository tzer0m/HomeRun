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
        /// The warm-up done before every session.
        /// </summary>
        public WarmUp WarmUp { get; set; } = new WarmUp();

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
            WarmUp = planService.Plan.WarmUp;
            CurrentWeek = planService.GetCurrentWeek(DateOnly.FromDateTime(DateTime.Now));
        }
    }
}