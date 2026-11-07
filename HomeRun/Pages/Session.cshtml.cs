using HomeRun.Models;
using HomeRun.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeRun.Pages
{
    /// <summary>
    /// Page model for the session runner.
    /// </summary>
    /// <param name="planService">The plan service.</param>
    /// <param name="options">The runner options.</param>
    public class SessionModel(PlanService planService, IOptions<RunnerOptions> options) : PageModel
    {
        /// <summary>
        /// The JSON options used to send the steps to the runner.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

        /// <summary>
        /// The workout being run, with progression applied.
        /// </summary>
        public Workout Workout { get; set; } = new Workout();

        /// <summary>
        /// The session steps as JSON.
        /// </summary>
        public string StepsJson { get; set; } = "[]";

        /// <summary>
        /// Builds the steps for the requested workout.
        /// </summary>
        /// <param name="id">The workout or stretching ID.</param>
        public IActionResult OnGet(string id)
        {
            Workout? workout = planService.GetWorkoutById(id, DateOnly.FromDateTime(DateTime.Now));
            if (workout == null || workout.Type == WorkoutType.Tick)
            {
                return NotFound();
            }
            Workout = workout;
            StepsJson = JsonSerializer.Serialize(workout.BuildSteps(options.Value.TransitionSeconds), JsonOptions);
            return Page();
        }
    }
}