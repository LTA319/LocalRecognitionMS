namespace ObjectRecognitionSystem.Models;

public class ItemInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Code { get; set; }
    public string? Category { get; set; }
    public string? Specification { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
}
