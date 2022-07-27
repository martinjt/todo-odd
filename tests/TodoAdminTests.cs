using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace tests;

public class TodoAdminTests
{
    [Fact]
    public async Task AddTodo_WithValidRequest_ReturnsOk()
    {
        var webapp = new WebApplicationFactory<Program>();
        var client = webapp.CreateClient();
        var addResponse = await client.PostAsJsonAsync("todo", new { 
            title = "New Todo",
            description = "Description of the todo"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, addResponse.StatusCode);
    }
}