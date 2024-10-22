using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace todo_odd.Tests;

public class CustomApplicationFactoryWithTelemetry : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(l => l.ClearProviders());
        builder.ConfigureServices((ctx, sp) =>
        {
            sp.Remove(sp.Single(s => s.ServiceType == typeof(DbContextOptions<TodoDbContext>)));
            sp.Remove(sp.Single(s => s.ServiceType == typeof(TodoDbContext)));
            sp.AddSingleton(new TodoDbContext(
                new DbContextOptionsBuilder<TodoDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
        });
    }
}
