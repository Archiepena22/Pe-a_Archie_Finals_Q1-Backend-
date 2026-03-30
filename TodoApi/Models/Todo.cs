namespace TodoApi.Models;

public class Todo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
}
