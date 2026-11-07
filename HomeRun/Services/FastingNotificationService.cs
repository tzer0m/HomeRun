using HomeRun.Models;
using t0m.Ting;

namespace HomeRun.Services
{
    /// <summary>
    /// Background service that sends a Ting notification when the eating window opens and closes, listing anything left to do when it closes.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="scopeFactory">Used to create a scope per notification, since the history service is scoped.</param>
    /// <param name="tingClient">The Ting notification client.</param>
    public class FastingNotificationService(PlanService planService, IServiceScopeFactory scopeFactory, TingClient tingClient) : BackgroundService
    {
        /// <summary>
        /// Waits for each eating window change and sends a notification, until the application shuts down.
        /// </summary>
        /// <param name="stoppingToken">Signals that the application is stopping.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FastingWindow fasting = planService.Plan.Fasting;
            while (!stoppingToken.IsCancellationRequested)
            {
                (DateTime time, bool opening) = GetNextChange(fasting);
                await Task.Delay(time - DateTime.Now + TimeSpan.FromSeconds(1), stoppingToken);
                if (opening)
                {
                    await tingClient.SendAsync("Eating Window Open", $"Eat until {fasting.End:HH:mm}.");
                }
                else
                {
                    string? body = await GetClosedBodyAsync(fasting);
                    if (body != null)
                    {
                        await tingClient.SendAsync("Eating Window Closed", body);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the closing notification body, listing today's items that are not done yet, or null if everything is done.
        /// </summary>
        /// <param name="fasting">The fasting window.</param>
        private async Task<string?> GetClosedBodyAsync(FastingWindow fasting)
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
            if (workout != null && day?.WorkoutDone != true)
            {
                pending.Add(workout.Name);
            }
            return pending.Count == 0 ? null : $"Fasting until {fasting.Start:HH:mm}. Still to do: {string.Join(", ", pending)}.";
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