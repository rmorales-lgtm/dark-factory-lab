using Microsoft.EntityFrameworkCore;

namespace Chronos.Web.Data;

public class ChronosDbContext : DbContext
{
    public ChronosDbContext(DbContextOptions<ChronosDbContext> options) : base(options)
    {
    }
}

public sealed record ChronosPaths(string DataDirectory);
