using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private static readonly List<Todo> _todos = new();
    private static readonly object _lock = new();
    private static readonly List<Channel<string>> _streams = new();

    [HttpGet]
    public IActionResult Get()
    {
        lock (_lock)
        {
            return Ok(_todos);
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] Todo todo)
    {
        if (todo == null || string.IsNullOrWhiteSpace(todo.Title))
        {
            return BadRequest("Title is required.");
        }

        string previousHash;
        lock (_lock)
        {
            previousHash = _todos.Count == 0 ? "GENESIS" : _todos[^1].Hash;
        }
        var newTodo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = todo.Title.Trim(),
            Completed = todo.Completed,
            PreviousHash = previousHash
        };

        newTodo.Hash = ComputeHash(newTodo);
        lock (_lock)
        {
            _todos.Add(newTodo);
        }
        BroadcastTodos();
        return Created($"/api/todos/{newTodo.Id}", newTodo);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Put(Guid id, [FromBody] Todo todo)
    {
        if (todo == null || string.IsNullOrWhiteSpace(todo.Title))
        {
            return BadRequest("Title is required.");
        }

        Todo? existing;
        lock (_lock)
        {
            existing = _todos.FirstOrDefault(t => t.Id == id);
        }
        if (existing == null)
        {
            return NotFound();
        }

        lock (_lock)
        {
            existing.Title = todo.Title.Trim();
            existing.Completed = todo.Completed;
            RebuildChainFromIndex(_todos.IndexOf(existing));
        }
        BroadcastTodos();

        return Ok(existing);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        Todo? existing;
        lock (_lock)
        {
            existing = _todos.FirstOrDefault(t => t.Id == id);
        }
        if (existing == null)
        {
            return NotFound();
        }

        lock (_lock)
        {
            _todos.Remove(existing);
            RebuildChainFromIndex(0);
        }
        BroadcastTodos();
        return NoContent();
    }

    [HttpGet("verify")]
    public IActionResult Verify()
    {
        lock (_lock)
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
        }

        return Ok(new { message = "Chain valid." });
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var channel = Channel.CreateUnbounded<string>();
        lock (_lock)
        {
            _streams.Add(channel);
        }

        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await Response.WriteAsync($"data: {message}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_lock)
            {
                _streams.Remove(channel);
            }
        }
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

    private static void BroadcastTodos()
    {
        string payload;
        lock (_lock)
        {
            payload = JsonSerializer.Serialize(_todos);
        }

        List<Channel<string>> streamsCopy;
        lock (_lock)
        {
            streamsCopy = _streams.ToList();
        }

        foreach (var stream in streamsCopy)
        {
            stream.Writer.TryWrite(payload);
        }
    }
}
