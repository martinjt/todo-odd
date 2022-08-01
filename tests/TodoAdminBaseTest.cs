using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using todo_odd;
using Microsoft.EntityFrameworkCore;

namespace tests;

public abstract class TodoAdminBaseTest
{
    public static object GetValidTodo(string title = null!, string description = "Do Something cool")
    {
        return new
        {
            title = title ?? "New Todo",
            description
        };
    }

    public static async Task<int> CreateValidToDoItem(HttpClient api, string title = null!)
    {
        var addResponse = await api.PostAsJsonAsync("todo", GetValidTodo(title));
        if (addResponse == null || 
            !addResponse.IsSuccessStatusCode)
            return -1;
        return (await addResponse.Content.ReadFromJsonAsync<AddTodoResponse>())?.Id ?? -1;
    }
}

public class CustomApplicationFactoryWithInMemoryDb : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(l => l.ClearProviders());
        builder.ConfigureServices(sp => {
            sp.Remove(sp.Single(s => s.ServiceType == typeof(DbContextOptions<TodoDbContext>)));
            sp.Remove(sp.Single(s => s.ServiceType == typeof(TodoDbContext)));
            sp.AddSingleton<TodoDbContext>(new TodoDbContext(
                new DbContextOptionsBuilder<TodoDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
        });
        base.ConfigureWebHost(builder);
    }
}
