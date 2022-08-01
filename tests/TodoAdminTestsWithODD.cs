using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using System.Net.Http.Json;
using Honeycomb.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Resources;
using Xunit.Abstractions;
using todo_odd;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace tests;

public class TodoAdminTestsWithODD : TodoAdminBaseTest, IAsyncLifetime
{
    internal CustomApplicationFactoryWithTelemetry _webApp;
    internal HttpClient _api;

    private static readonly List<Activity> CollectedSpans = new List<Activity>();

    private TelemetrySpan TestSpan;

    public void RecordTestName([CallerMemberName]string functionName = null!) => TestSpan.UpdateName($"Test: {functionName}");

    public TodoAdminTestsWithODD(ITestOutputHelper testOutputHelper) : base()
    {
        //        if (_webApp == null)
        _webApp = new CustomApplicationFactoryWithTelemetry(CollectedSpans, testOutputHelper);

        //        if (_api == null)
        _api = _webApp.CreateClient();

        var tracer = _webApp.TracerProvider.GetTracer("test-fact");
        TestSpan = tracer.StartRootSpan("Test started");

        testOutputHelper.WriteLine($"TraceId: {TestSpan.Context.TraceId.ToString()}");
        _api.DefaultRequestHeaders.Add("traceparent", $"00-{TestSpan.Context.TraceId.ToString()}-{TestSpan.Context.SpanId.ToString()}-01");
    }

    //[Fact]
    public async Task AddTodo_WithValidRequest_ProducesSaveTelemetry()
    {
        var addResponse = await CreateValidToDoItem(_api);

        var saveActivity = CollectedSpans.FirstOrDefault(
            a =>
            {
                if (a.DisplayName.Contains("Save"))
                    return false;

                var tagDict = a.Tags.ToDictionary(t => t.Key, t => t.Value);
                return tagDict.TryGetValue("title", out var title) && title == "New Todo";
            }
        );
        Assert.NotNull(saveActivity);
    }

    [Fact]
    public async Task GetTodos_WithCachedData_DoesNotCallDatabase()
    {
        RecordTestName();
        var getAll = await _api.GetFromJsonAsync<List<TodoItem>>("todo-list");
        CollectedSpans.RemoveAll(x => true);

        var getAllCached = await _api.GetFromJsonAsync<List<TodoItem>>("todo-list");

        var cacheActivity = CollectedSpans.FirstOrDefault(
            a =>
                a.DisplayName.Contains("get-todo-list-from-db"));
        Assert.Null(cacheActivity);
    }

    [Fact]
    public async Task GetTodos_FirstCall_CallsTheDatabase()
    {
        RecordTestName();
        var getAll = await _api.GetAsync("todo-list");

        // ???
    }

    [Fact]
    public async Task GetTodos_FirstCall_CallsTheDatabase_V1()
    {
        RecordTestName();
        var getAll = await _api.GetAsync("todo-list");

        var cacheActivity = CollectedSpans.FirstOrDefault(
            a =>
                a.DisplayName.Contains("get-todo-list-from-db"));
        Assert.NotNull(cacheActivity);
    }

    public Task InitializeAsync()
    {
        CollectedSpans.RemoveAll(x => true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        TestSpan.End();
        await _webApp.DisposeAsync();
    }
}

public class CustomApplicationFactoryWithTelemetry : WebApplicationFactory<Program>
{
    public TracerProvider TracerProvider { get; }
    public static string TestRunId = Guid.NewGuid().ToString();
    public CustomApplicationFactoryWithTelemetry(List<Activity> spans, ITestOutputHelper outputHelper)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json"))
            .AddEnvironmentVariables()
            .Build(); 
            
        var honeycombOption = configuration.GetSection("Honeycomb").Get<HoneycombOptions>();
        TracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("test-fact")
            .AddSource(ActivityHelper.Source.Name)
            .AddProcessor(new TestRunSpanProcessor())
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("todo-odd-tests-new2"))
            .AddAspNetCoreInstrumentation()
            .AddInMemoryExporter(spans)
            .AddProcessor(new SimpleActivityExportProcessor(new ActivityExporter(outputHelper)))
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(honeycombOption.TracesEndpoint);
                otlpOptions.Headers = honeycombOption.GetTraceHeaders();
                otlpOptions.ExportProcessorType = ExportProcessorType.Simple;
            })
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json"), false);
        });
        builder.ConfigureLogging(l => l.ClearProviders());
        builder.ConfigureServices((ctx, sp) =>
        {
            sp.Remove(sp.Single(s => s.ServiceType == typeof(DbContextOptions<TodoDbContext>)));
            sp.Remove(sp.Single(s => s.ServiceType == typeof(TodoDbContext)));
            sp.AddSingleton<TodoDbContext>(new TodoDbContext(
                new DbContextOptionsBuilder<TodoDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));

            sp.AddSingleton(TracerProvider);
        });
    }

    public override ValueTask DisposeAsync()
    {
        TracerProvider.Dispose();
        return base.DisposeAsync();
    }
}

public class ActivityExporter : BaseExporter<Activity>
{
    public ITestOutputHelper _testOutputHelper;

    public ActivityExporter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
            _testOutputHelper.WriteLine($"New Activity: {activity.Id}");

        return ExportResult.Success;
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

static class HoneycombOptionsExtensions
{

    internal static string GetTraceHeaders(this HoneycombOptions options)
    {
        var headers = new List<string>
            {
                "x-otlp-version=0.16.0",
                $"x-honeycomb-team={options.TracesApiKey}"
            };
        return string.Join(",", headers);
    }
}
