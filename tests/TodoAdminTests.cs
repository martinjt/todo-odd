using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenTelemetry;

namespace todo_odd.Tests;

public class TodoAdminTests : TodoAdminBaseTest
{
    internal WebApplicationFactory<Program> _webApp;
    internal HttpClient _api;

    public TodoAdminTests()
    {
        if (_webApp == null)
            _webApp = new CustomApplicationFactoryWithInMemoryDb();
        
        if (_api == null)
            _api = _webApp.CreateClient();
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_ReturnsOk()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var webapp = new CustomApplicationFactoryWithInMemoryDb();
        var client = webapp.CreateClient();
        var addResponse = await client.PostAsJsonAsync("todo", new { 
            title = "New Todo",
            description = "Description of the todo"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_ReturnsOk_V2()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var addResponse = await _api.PostAsJsonAsync("todo", new
        {
            title = "New Todo",
            description = "Description of the todo"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, addResponse.StatusCode); 
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_ReturnsOk_V3()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var todoItem = new
        {
            title = "New Todo",
            description = "Description of the todo"
        };

        var addResponse = await _api.PostAsJsonAsync("todo", todoItem);

        Assert.Equal(System.Net.HttpStatusCode.OK, addResponse.StatusCode); 
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_ReturnsOk_V4()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var validTodoItem = GetValidTodo();

        var addResponse = await _api.PostAsJsonAsync("todo", validTodoItem);

        Assert.Equal(System.Net.HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_HasValidId()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var validTodoItem = GetValidTodo();

        var response = await _api.PostAsJsonAsync("todo", validTodoItem);

        var addedTodoItemResponse = await response.Content.ReadFromJsonAsync<AddTodoResponse>();
        Assert.NotNull(addedTodoItemResponse?.Id);
        Assert.InRange(addedTodoItemResponse.Id, 1, int.MaxValue);
    }

    [Fact]
    public async Task AddTodo_WithValidRequest_CanBeRetrievedById()
    {
        using var scope = SuppressInstrumentationScope.Begin();
        var todoItemId = await CreateValidToDoItem(_api);

        var todoItem = await _api.GetFromJsonAsync<TodoItem>($"todo/{todoItemId}");
        Assert.Equal(todoItemId, todoItem!.Id);
    }
}

internal class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}

internal class AddTodoResponse
{
    public int Id { get; set; }
}