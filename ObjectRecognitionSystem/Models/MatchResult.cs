namespace ObjectRecognitionSystem.Models;

public class MatchResult
{
    public ItemInfo? Item { get; set; }
    public string MatchMethod { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public List<ItemInfo> Candidates { get; set; } = new();
}
