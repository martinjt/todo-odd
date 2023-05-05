using Microsoft.AspNetCore.Mvc;
using todo_odd;

[Route("todo")]
public class TodoController : ControllerBase
{
    private readonly TodoDbContext _context;


    public TodoController(TodoDbContext context)
    {
        _context = context;
    }

    [HttpPost()]
    public ActionResult AddTodo([FromBody]AddTodoItem item)
    {
        var todoItem = new TodoItem {
            Title = item.Title,
            Description = item.Description
        };
        _context.TodoItems.Add(todoItem);
        
        using var span = ActivityHelper.Source.StartActivity("save-todo");
        span?.AddTag("todo.title", item.Title);

        _context.SaveChanges();
        return Ok(new {
            id = todoItem.Id
        });
    }


    [HttpGet("{id}")]
    public ActionResult<TodoItem> GetById([FromRoute]int id)
    {
        
        var item = _context.TodoItems.FirstOrDefault(t => t.Id == id);
        if (item == null)
            return NotFound();
        
        return item;
    }

    [HttpGet]
    public ActionResult<List<TodoItem>> GetAll()
    {
        return _context.TodoItems.ToList();
    }

}

public class AddTodoItem
{
    public string Title { get; set; }
    public string Description { get; set; }
}