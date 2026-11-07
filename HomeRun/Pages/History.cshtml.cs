using HomeRun.Models;
using HomeRun.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeRun.Pages
{
    /// <summary>
    /// Page model for the history page.
    /// </summary>
    /// <param name="history">The history service.</param>
    public class HistoryModel(HistoryService history) : PageModel
    {
        /// <summary>
        /// Every recorded day, newest first.
        /// </summary>
        public List<HistoryDay> Days { get; set; } = [];

        /// <summary>
        /// Today's date, whose unfinished items are still pending.
        /// </summary>
        public DateOnly Today { get; set; }

        /// <summary>
        /// The current streak of complete days.
        /// </summary>
        public int Streak { get; set; }

        /// <summary>
        /// The longest streak of complete days.
        /// </summary>
        public int BestStreak { get; set; }

        /// <summary>
        /// Loads the history and streaks.
        /// </summary>
        public async Task OnGetAsync()
        {
            Today = DateOnly.FromDateTime(DateTime.Now);
            List<HistoryDay> days = await history.GetDaysAsync(Today);
            Streak = HistoryService.GetStreak(days);
            BestStreak = HistoryService.GetBestStreak(days);
            days.Reverse();
            Days = days;
        }
    }
}