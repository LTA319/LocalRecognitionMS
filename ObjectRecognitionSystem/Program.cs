using ObjectRecognitionSystem.Forms;
using ObjectRecognitionSystem.Services;

namespace ObjectRecognitionSystem;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            // 启动前校验 ONNX 模型文件，避免进入主界面后才报错
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfig.OnnxModelPath);
            if (!File.Exists(modelPath))
            {
                MessageBox.Show(
                    $"ONNX 模型文件未找到:\n{modelPath}\n\n请将模型文件放入 Assets/models/ 目录。",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 预热 ONNX Runtime：跑一次空图推理触发 JIT 编译，避免首次识别卡顿
            DetectionService? detection = null;
            try
            {
                detection = new DetectionService();
                detection.WarmUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"模型加载失败: {ex.Message}",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                detection?.Dispose();
            }

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"系统启动失败: {ex.Message}",
                "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
