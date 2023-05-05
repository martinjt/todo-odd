using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using todo_odd;

[Route("todo-list")]
public class TodoListController : ControllerBase
{
    private readonly TodoDbContext _context;
    private readonly IMemoryCache _memoryCache;

    public TodoListController(TodoDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    public ActionResult<List<TodoItem>> GetAll()
    {
        const string todoListKey = "todo-list";

        if (_memoryCache.TryGetValue<List<TodoItem>>(todoListKey, out var todoList))
            return todoList;

        using (var span = ActivityHelper.Source.StartActivity("get-todo-list-from-db"))
        {
            var allTodos = _context.TodoItems.ToList();

            _memoryCache.Set(todoListKey, allTodos);

            return allTodos;
        }
    }

}