namespace ObjectRecognitionSystem.Models;

public class NameMapping
{
    public int Id { get; set; }
    public string DetectedName { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string? Category { get; set; }
}
