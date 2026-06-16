using SkiaSharp;
using ObjectRecognitionSystem.Models;
using YoloDotNet;
using YoloDotNet.Enums;
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
        using var surface = SKSurface.Create(new SKImageInfo(640, 640));
        var image = surface.Snapshot();
        _yolo.RunObjectDetection(image, _confidenceThreshold, 0.5);
    }

    public Task<List<DetectionResult>> DetectAsync(Bitmap image)
    {
        return Task.Run(() =>
        {
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0;
            using var skImage = SKImage.FromEncodedData(ms);

            var results = _yolo.RunObjectDetection(skImage, _confidenceThreshold, 0.5);
            return results
                .Select(d => new DetectionResult
                {
                    Name = d.Label.Name,
                    Confidence = d.Confidence,
                    BoundingBox = new Rectangle(
                        d.BoundingBox.Left, d.BoundingBox.Top,
                        d.BoundingBox.Width, d.BoundingBox.Height)
                })
                .ToList();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _yolo.Dispose();
    }
}
