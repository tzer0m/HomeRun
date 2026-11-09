using HomeRun.Data;
using HomeRun.Models;
using HomeRun.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HomeRun.Endpoints
{
    /// <summary>
    /// The API key protected endpoint that feeds the Tidgets home screen widget.
    /// </summary>
    public static class WidgetEndpoint
    {
        /// <summary>
        /// Returns today's tasks and whether each is done as JSON, with an activity task done by the workout or padel, or 401 if the API key is missing or wrong.
        /// </summary>
        /// <param name="request">The HTTP request, for the API key header.</param>
        /// <param name="planService">The plan service.</param>
        /// <param name="db">The database context.</param>
        /// <param name="configuration">Used to read the expected API key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task<IResult> GetAsync(HttpRequest request, PlanService planService, AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken)
        {
            string? expected = configuration["Widget:ApiKey"];
            string provided = request.Headers["X-Api-Key"].ToString();
            if (string.IsNullOrEmpty(expected) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
            {
                return Results.Unauthorized();
            }
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            HashSet<string> done = (await db.Checklist.Where(x => x.Date == today).Select(x => x.Item).ToListAsync(cancellationToken)).ToHashSet();
            Workout? workout = planService.GetWorkout(today);
            WidgetResponse response = new() { Tasks = [new WidgetTask { Name = "Weigh In", Done = await db.Weights.AnyAsync(x => x.Date == today, cancellationToken) }, new WidgetTask { Name = planService.Plan.Stretching.Name, Done = done.Contains(planService.Plan.Stretching.Id) }] };
            if (workout != null && workout.Type != WorkoutType.Rest)
            {
                response.Tasks.Add(new WidgetTask { Name = "Activity", Done = done.Contains(workout.Id) || done.Contains(planService.Plan.Padel.Id) });
            }
            return Results.Ok(response);
        }
    }
}