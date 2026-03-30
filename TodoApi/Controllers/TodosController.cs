using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private static readonly List<Todo> _todos = new();

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_todos);
    }

    [HttpPost]
    public IActionResult Post([FromBody] Todo todo)
    {
        if (todo == null || string.IsNullOrWhiteSpace(todo.Title))
        {
            return BadRequest("Title is required.");
        }

        var newTodo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = todo.Title.Trim(),
            Completed = todo.Completed
        };

        _todos.Add(newTodo);
        return Created($"/api/todos/{newTodo.Id}", newTodo);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Put(Guid id, [FromBody] Todo todo)
    {
        if (todo == null || string.IsNullOrWhiteSpace(todo.Title))
        {
            return BadRequest("Title is required.");
        }

        var existing = _todos.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Title = todo.Title.Trim();
        existing.Completed = todo.Completed;

        return Ok(existing);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var existing = _todos.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        _todos.Remove(existing);
        return NoContent();
    }
}
