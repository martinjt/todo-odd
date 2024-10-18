using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using todo_odd;
using Microsoft.EntityFrameworkCore;

namespace todo_odd.Tests;

public class CustomApplicationFactoryWithTelemetry : WebApplicationFactory<Program>
{
    public CustomApplicationFactoryWithTelemetry()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json"))
            .AddEnvironmentVariables()
            .Build(); 
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(l => l.ClearProviders());
        builder.ConfigureServices((ctx, sp) =>
        {
            sp.Remove(sp.Single(s => s.ServiceType == typeof(DbContextOptions<TodoDbContext>)));
            sp.Remove(sp.Single(s => s.ServiceType == typeof(TodoDbContext)));
            sp.AddSingleton<TodoDbContext>(new TodoDbContext(
                new DbContextOptionsBuilder<TodoDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
        });
    }
}

public class TestRunSpanProcessor : BaseProcessor<Activity>
{
    public static string TestRunId { get; } = Guid.NewGuid().ToString();

    public override void OnStart(Activity data)
    {
        data?.SetTag("test.run_id", TestRunId);
    }
}