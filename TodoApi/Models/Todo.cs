namespace TodoApi.Models;

public class Todo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string PreviousHash { get; set; } = string.Empty;
}
