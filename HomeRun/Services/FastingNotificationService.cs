using HomeRun.Models;
using t0m.Ting;

namespace HomeRun.Services
{
    /// <summary>
    /// Background service that sends a Ting notification when the eating window opens and closes, adding anything still to do to the closing one, or only sending what is still to do when fasting is off.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="scopeFactory">Used to create a scope per notification, since the history and settings services are scoped.</param>
    /// <param name="tingClient">The Ting notification client.</param>
    /// <param name="configuration">The configuration, used for the site URL opened when a notification is tapped.</param>
    public class FastingNotificationService(PlanService planService, IServiceScopeFactory scopeFactory, TingClient tingClient, IConfiguration configuration) : BackgroundService
    {
        /// <summary>
        /// Waits for each eating window change and sends the matching notification, until the application shuts down.
        /// </summary>
        /// <param name="stoppingToken">Signals that the application is stopping.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FastingWindow fasting = planService.Plan.Fasting;
            string? siteUrl = configuration["SiteUrl"];
            while (!stoppingToken.IsCancellationRequested)
            {
                (DateTime time, bool opening) = GetNextChange(fasting);
                await Task.Delay(time - DateTime.Now + TimeSpan.FromSeconds(1), stoppingToken);
                using IServiceScope scope = scopeFactory.CreateScope();
                bool enabled = (await scope.ServiceProvider.GetRequiredService<SettingsService>().GetAsync()).FastingEnabled;
                if (opening)
                {
                    if (enabled)
                    {
                        await tingClient.SendAsync("HomeRun: Eating Window Open", $"Eat until {fasting.End:HH:mm}.", siteUrl);
                    }
                }
                else
                {
                    List<string> pending = await GetPendingAsync(scope.ServiceProvider.GetRequiredService<HistoryService>());
                    string todo = pending.Count == 0 ? string.Empty : $"Still to do: {string.Join(", ", pending)}.";
                    if (enabled)
                    {
                        string body = $"Fasting until {fasting.Start:HH:mm}.";
                        await tingClient.SendAsync("HomeRun: Eating Window Closed", todo.Length == 0 ? body : $"{body} {todo}", siteUrl);
                    }
                    else if (todo.Length > 0)
                    {
                        await tingClient.SendAsync("HomeRun: Still To Do", todo, siteUrl);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the names of today's items that are not done yet, where padel covers the workout.
        /// </summary>
        /// <param name="history">The history service.</param>
        private async Task<List<string>> GetPendingAsync(HistoryService history)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            List<HistoryDay> days = await history.GetDaysAsync(today);
            HistoryDay? day = days.Count > 0 ? days[^1] : null;
            Workout? workout = planService.GetWorkout(today);
            List<string> pending = [];
            if (day?.Kilograms == null)
            {
                pending.Add("Weigh In");
            }
            if (day?.Stretching != true)
            {
                pending.Add(planService.Plan.Stretching.Name);
            }
            if (workout != null && workout.Type != WorkoutType.Rest && day?.WorkoutDone != true && day?.Padel != true)
            {
                pending.Add(workout.Name);
            }
            return pending;
        }

        /// <summary>
        /// Finds when the eating window next opens or closes.
        /// </summary>
        /// <param name="fasting">The fasting window.</param>
        private static (DateTime Time, bool Opening) GetNextChange(FastingWindow fasting)
        {
            DateTime now = DateTime.Now;
            DateTime start = now.Date.Add(fasting.Start.ToTimeSpan());
            DateTime end = now.Date.Add(fasting.End.ToTimeSpan());
            if (now < start)
            {
                return (start, true);
            }
            return now < end ? (end, false) : (start.AddDays(1), true);
        }
    }
}