using HomeRun.Models;
using t0m.Ting;

namespace HomeRun.Services
{
    /// <summary>
    /// Background service that sends a Ting notification every time the eating window opens and closes, adding anything still to do to the closing one.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="scopeFactory">Used to create a scope per notification, since the history service is scoped.</param>
    /// <param name="tingClient">The Ting notification client.</param>
    /// <param name="configuration">The configuration, used for the site URL opened when a notification is tapped.</param>
    public class FastingNotificationService(PlanService planService, IServiceScopeFactory scopeFactory, TingClient tingClient, IConfiguration configuration) : BackgroundService
    {
        /// <summary>
        /// Waits for each eating window change and always sends the matching notification, until the application shuts down.
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
                if (opening)
                {
                    await tingClient.SendAsync("HomeRun: Eating Window Open", $"Eat until {fasting.End:HH:mm}.", siteUrl);
                }
                else
                {
                    string body = await GetClosedBodyAsync(fasting);
                    await tingClient.SendAsync("HomeRun: Eating Window Closed", body, siteUrl);
                }
            }
        }

        /// <summary>
        /// Builds the closing notification body with the fasting end time, plus today's items that are not done yet if there are any.
        /// </summary>
        /// <param name="fasting">The fasting window.</param>
        private async Task<string> GetClosedBodyAsync(FastingWindow fasting)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            using IServiceScope scope = scopeFactory.CreateScope();
            HistoryService history = scope.ServiceProvider.GetRequiredService<HistoryService>();
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
            if (workout != null && workout.Type != WorkoutType.Rest && day?.WorkoutDone != true)
            {
                pending.Add(workout.Name);
            }
            string body = $"Fasting until {fasting.Start:HH:mm}.";
            return pending.Count == 0 ? body : $"{body} Still to do: {string.Join(", ", pending)}.";
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