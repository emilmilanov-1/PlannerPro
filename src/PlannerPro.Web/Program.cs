using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Data;
using PlannerPro.Web.Services.Priorities;
using PlannerPro.Web.Services.Holidays;
using PlannerPro.Web.Services.Scheduling;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<IPriorityScoringService, PriorityScoringService>();
builder.Services.AddScoped<IScheduleOptimizerService, ScheduleOptimizerService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IHolidayApiClient, HolidayApiClient>(client =>
{
    client.BaseAddress = new Uri("https://nagerholidays.com/api/v4/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PlannerPro/1.0");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
