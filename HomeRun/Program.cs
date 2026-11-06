using HomeRun.Services;

// Create web application builder.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.Configure<PlanOptions>(builder.Configuration.GetSection("Plan"));
builder.Services.AddSingleton<PlanService>();

// Create the web application and configure.
WebApplication app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();