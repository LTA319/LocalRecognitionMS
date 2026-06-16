namespace ObjectRecognitionSystem.Models;

public class DetectionResult
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Rectangle BoundingBox { get; set; }
}
