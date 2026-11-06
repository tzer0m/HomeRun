namespace HomeRun.Services
{
    /// <summary>
    /// Settings for loading the workout plan.
    /// </summary>
    public class PlanOptions
    {
        /// <summary>
        /// The path to plan.json, relative to the application directory.
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }
}