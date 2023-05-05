using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using Honeycomb.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Resources;
using todo_odd;
using Microsoft.EntityFrameworkCore;

namespace tests;

public class CustomApplicationFactoryWithTelemetry : WebApplicationFactory<Program>
{
    private const string TestTracerName = "todo-odd-example";
    private readonly TracerProvider _tracerProvider;
    public Tracer TestTracer { get; set; }
    public static string TestRunId = Guid.NewGuid().ToString();
    public CustomApplicationFactoryWithTelemetry(List<Activity> spans)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json"))
            .AddEnvironmentVariables()
            .Build(); 

        var honeycombOption = configuration.GetSection("Honeycomb").Get<HoneycombOptions>();
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(TestTracerName)
            .AddSource(ActivityHelper.Source.Name)
            .AddProcessor(new TestRunSpanProcessor())
            .ConfigureResource(r => r.AddService(TestTracerName))
            .AddAspNetCoreInstrumentation()
            .AddInMemoryExporter(spans)
            .AddHoneycomb(honeycombOption)
            .Build();

        TestTracer = _tracerProvider.GetTracer(TestTracerName);
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

            sp.AddSingleton(_tracerProvider);
        });
    }

    public override ValueTask DisposeAsync()
    {
        _tracerProvider.ForceFlush();
        _tracerProvider.Dispose();
        return base.DisposeAsync();
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