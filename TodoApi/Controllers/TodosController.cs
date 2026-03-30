using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
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

        var previousHash = _todos.Count == 0 ? "GENESIS" : _todos[^1].Hash;
        var newTodo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = todo.Title.Trim(),
            Completed = todo.Completed,
            PreviousHash = previousHash
        };

        newTodo.Hash = ComputeHash(newTodo);
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
        RebuildChainFromIndex(_todos.IndexOf(existing));

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
        RebuildChainFromIndex(0);
        return NoContent();
    }

    [HttpGet("verify")]
    public IActionResult Verify()
    {
        for (var i = 0; i < _todos.Count; i++)
        {
            var current = _todos[i];
            var expectedPreviousHash = i == 0 ? "GENESIS" : _todos[i - 1].Hash;
            if (!string.Equals(current.PreviousHash, expectedPreviousHash, StringComparison.Ordinal))
            {
                return Conflict(new { message = "Chain tampered: previous hash mismatch." });
            }

            var expectedHash = ComputeHash(current);
            if (!string.Equals(current.Hash, expectedHash, StringComparison.Ordinal))
            {
                return Conflict(new { message = "Chain tampered: hash mismatch." });
            }
        }

        return Ok(new { message = "Chain valid." });
    }

    private static void RebuildChainFromIndex(int startIndex)
    {
        if (startIndex < 0)
        {
            return;
        }

        for (var i = startIndex; i < _todos.Count; i++)
        {
            _todos[i].PreviousHash = i == 0 ? "GENESIS" : _todos[i - 1].Hash;
            _todos[i].Hash = ComputeHash(_todos[i]);
        }
    }

    private static string ComputeHash(Todo todo)
    {
        var payload = $"{todo.Id}|{todo.Title}|{todo.Completed}|{todo.PreviousHash}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = SHA256.HashData(bytes);
        var builder = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
