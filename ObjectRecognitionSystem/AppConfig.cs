using System.Configuration;

namespace ObjectRecognitionSystem;

public static class AppConfig
{
    public static string ConnectionString =>
        ConfigurationManager.AppSettings["ConnectionString"] ?? "Data Source=items.db";

    public static string OnnxModelPath =>
        ConfigurationManager.AppSettings["OnnxModelPath"] ?? "Assets/models/yolo11n.onnx";

    public static double ConfidenceThreshold =>
        double.TryParse(ConfigurationManager.AppSettings["ConfidenceThreshold"], out var v) ? v : 0.5;

    public static bool CudaEnabled =>
        bool.TryParse(ConfigurationManager.AppSettings["CudaEnabled"], out var v) && v;

    public static int AutoDetectIntervalMs =>
        int.TryParse(ConfigurationManager.AppSettings["AutoDetectIntervalMs"], out var v) ? v : 1500;

    public static int PreviewFps =>
        int.TryParse(ConfigurationManager.AppSettings["PreviewFps"], out var v) ? v : 30;

    public static bool SemanticMatchEnabled =>
        bool.TryParse(ConfigurationManager.AppSettings["SemanticMatchEnabled"], out var v) && v;

    public static int FuzzyThreshold =>
        int.TryParse(ConfigurationManager.AppSettings["FuzzyThreshold"], out var v) ? v : 70;

    public static int SearchTopN =>
        int.TryParse(ConfigurationManager.AppSettings["SearchTopN"], out var v) ? v : 5;
}
