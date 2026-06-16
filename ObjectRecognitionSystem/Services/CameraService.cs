using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ObjectRecognitionSystem.Services;

public class CameraService : IDisposable
{
    private VideoCapture? _capture;
    private System.Threading.Timer? _previewTimer;
    private PictureBox? _previewBox;
    private bool _disposed;

    public void StartCamera(PictureBox pbPreview)
    {
        _previewBox = pbPreview;
        _capture = new VideoCapture(0);

        var interval = 1000 / AppConfig.PreviewFps;
        _previewTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed || _capture == null || _previewBox == null) return;

            try
            {
                using var frame = _capture.QueryFrame();
                if (frame == null) return;

                using var image = frame.ToImage<Bgr, byte>();
                var oldImage = _previewBox.Image;
                _previewBox.Image = image.ToBitmap();
                oldImage?.Dispose();
            }
            catch
            {
                // Silently skip dropped frames
            }
        }, null, 0, interval);
    }

    public Bitmap? GrabFrame()
    {
        if (_capture == null) return null;

        using var frame = _capture.QueryFrame();
        if (frame == null) return null;

        using var image = frame.ToImage<Bgr, byte>();
        return image.ToBitmap();
    }

    public void StopCamera()
    {
        _previewTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _previewTimer?.Dispose();
        _previewTimer = null;

        _capture?.Dispose();
        _capture = null;

        if (_previewBox?.Image != null)
        {
            _previewBox.Image.Dispose();
            _previewBox.Image = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCamera();
    }
}
