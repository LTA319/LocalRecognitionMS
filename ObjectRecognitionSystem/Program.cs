using ObjectRecognitionSystem.Forms;

namespace ObjectRecognitionSystem;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
