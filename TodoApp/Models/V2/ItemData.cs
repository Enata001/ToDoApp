namespace TodoApp.Models;

public class ItemDataV2
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool Done { get; set; }
}