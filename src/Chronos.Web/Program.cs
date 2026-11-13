using Chronos.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<ChronosDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var path = config["Chronos:DatabasePath"];
    if (string.IsNullOrWhiteSpace(path))
    {
        path = Path.Combine(env.ContentRootPath, "chronos.db");
    }
    options.UseSqlite($"Data Source={path}");
});
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new ChronosPaths(ResolveDataDirectory(config["Chronos:DataDirectory"], env.ContentRootPath));
});
builder.Services.AddSingleton<Chronos.Web.Domain.IWeekDataSource>(sp =>
    new Chronos.Web.Domain.FileWeekDataSource(sp.GetRequiredService<ChronosPaths>()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChronosDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/NotFound");

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self' 'unsafe-inline'";
        return Task.CompletedTask;
    });
    await next();
});

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static string ResolveDataDirectory(string? configured, string contentRoot)
{
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured;
    }
    var dir = new DirectoryInfo(contentRoot);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "data");
        if (File.Exists(Path.Combine(candidate, "week1.json")))
        {
            return candidate;
        }
        dir = dir.Parent;
    }
    return Path.Combine(contentRoot, "data");
}

public partial class Program { }
