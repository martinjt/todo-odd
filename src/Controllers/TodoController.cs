using Microsoft.AspNetCore.Mvc;

[Route("todo")]
public class TodoController : ControllerBase
{
    [HttpPost()]
    public ActionResult AddTodo()
    {
        return Ok();
    }
}