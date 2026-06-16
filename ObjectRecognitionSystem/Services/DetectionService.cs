using System.Drawing;
using ObjectRecognitionSystem.Models;
using YoloDotNet;
using YoloDotNet.Models;

namespace ObjectRecognitionSystem.Services;

public class DetectionService : IDisposable
{
    private readonly Yolo _yolo;
    private readonly double _confidenceThreshold;
    private bool _disposed;

    public DetectionService()
    {
        var options = new YoloOptions
        {
            OnnxModel = AppConfig.OnnxModelPath,
            ModelType = ModelType.ObjectDetection,
            Cuda = AppConfig.CudaEnabled
        };
        _yolo = new Yolo(options);
        _confidenceThreshold = AppConfig.ConfidenceThreshold;
    }

    public void WarmUp()
    {
        using var dummy = new Bitmap(640, 640);
        using var g = Graphics.FromImage(dummy);
        g.Clear(Color.Black);
        _yolo.Detect(dummy);
    }

    public Task<List<DetectionResult>> DetectAsync(Bitmap image)
    {
        return Task.Run(() =>
        {
            var results = _yolo.Detect(image)
                .Where(d => d.Confidence >= _confidenceThreshold)
                .OrderByDescending(d => d.Confidence)
                .Select(d => new DetectionResult
                {
                    Name = d.Name,
                    Confidence = d.Confidence,
                    BoundingBox = new Rectangle(
                        d.BoundingBox.X, d.BoundingBox.Y,
                        d.BoundingBox.Width, d.BoundingBox.Height)
                })
                .ToList();
            return results;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _yolo.Dispose();
    }
}
