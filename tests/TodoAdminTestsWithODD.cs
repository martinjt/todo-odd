using System.Diagnostics;
using OpenTelemetry.Trace;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace tests;

public class TodoAdminTestsWithODD : TodoAdminBaseTest, IAsyncLifetime
{
    private CustomApplicationFactoryWithTelemetry _webApp;
    private HttpClient _api;

    private static readonly List<Activity> CollectedSpans = new List<Activity>();
    private TelemetrySpan TestSpan;

    public void RecordTestName([CallerMemberName]string functionName = null!) => TestSpan.UpdateName($"Test: {functionName}");
    public Task InitializeAsync()
    {
        CollectedSpans.RemoveAll(x => true);
        return Task.CompletedTask;
    }

    public TodoAdminTestsWithODD() : base()
    {
        _webApp = new CustomApplicationFactoryWithTelemetry(CollectedSpans);
        _api = _webApp.CreateClient();

        TestSpan = _webApp.TestTracer.StartRootSpan("Test started");

        _api.DefaultRequestHeaders.Add("traceparent", 
            $"00-{TestSpan.Context.TraceId.ToString()}-{TestSpan.Context.SpanId.ToString()}-01");
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_ProducesSaveTelemetry()
    {
        var addResponse = await CreateValidToDoItem(_api);

        var saveActivity = CollectedSpans.FirstOrDefault(
            a => a.DisplayName.Contains("save-todo"));
        Assert.NotNull(saveActivity);

        var titleTag = saveActivity.Tags.FirstOrDefault(t => t.Key == "todo.title");
        
        Assert.Equal("New Todo", titleTag.Value);
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
    public async Task GetTodos_FirstCall_CallsTheDatabase_V2()
    {
        RecordTestName();
        var getAll = await _api.GetAsync("todo-list");

        var cacheActivity = CollectedSpans.FirstOrDefault(
            a =>
                a.DisplayName.Contains("get-todo-list-from-db"));
        Assert.NotNull(cacheActivity);
    }

    [Fact]
    public async Task GetTodos_FirstCall_CallsTheDatabase_V3()
    {
        var getAll = await _api.GetAsync("todo-list");

        CollectedSpans.HasSpanWithName("get-todo-list-from-db");
    }


    [Fact]
    public async Task GetTodos_AsyncBatching_IsParallel()
    {
        var getAll = await _api.GetAsync("todo-list");

        var rootSpan = CollectedSpans.WithName("start-get")
            .FirstOrDefault();
        var processingSpans = CollectedSpans.WithName("get-from-db");

        foreach (var span in processingSpans)
            Assert.Equal(span.ParentId, rootSpan.Id);
    }

    public async Task DisposeAsync()
    {
        TestSpan.End();
        await _webApp.DisposeAsync();
    }

}

public static class AssertSpan
{
    public static void HasSpanWithName(this List<Activity> activities, string Name)
    {
        var foundActivity = activities.FirstOrDefault(
            a =>
                a.DisplayName.Contains(Name));
        Assert.NotNull(foundActivity);
    }
    public static void DoesNotSpanWithName(this List<Activity> activities, string Name)
    {
        var foundActivity = activities.FirstOrDefault(
            a =>
                a.DisplayName.Contains(Name));
        Assert.Null(foundActivity);
    }

    public static IEnumerable<Activity> WithName(this List<Activity> activities, string Name)
    {
        return  activities.Where(
            a =>
                a.DisplayName.Contains(Name));
    }
}
