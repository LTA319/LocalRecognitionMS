using System.Data;
using Microsoft.Extensions.Logging;
using ObjectRecognitionSystem.Models;
using ObjectRecognitionSystem.Services;

namespace ObjectRecognitionSystem.Forms;

public partial class MainForm : Form
{
    private readonly CameraService _camera;
    private readonly DetectionService _detection;
    private readonly MatchingService _matching;
    private readonly DatabaseService _database;
    private readonly ILogger<MainForm> _logger;

    private System.Windows.Forms.Timer? _autoTimer;
    private int _detecting;

    public MainForm()
    {
        InitializeComponent();

        _logger = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information))
            .CreateLogger<MainForm>();

        _database = new DatabaseService();
        _database.EnsureCreated();
        _database.LoadCacheAsync().GetAwaiter().GetResult();

        _detection = new DetectionService();
        _matching = new MatchingService(_database);
        _camera = new CameraService();

        btnCapture.Click += async (_, _) => await DoDetectAsync();
        cbAutoDetect.CheckedChanged += (_, _) =>
        {
            if (cbAutoDetect.Checked)
                StartAutoDetect();
            else
                StopAutoDetect();
        };
        btnFeedback.Click += async (_, _) => await DoFeedbackAsync();

        this.FormClosing += MainForm_FormClosing;
    }

    private async Task DoDetectAsync()
    {
        if (Interlocked.CompareExchange(ref _detecting, 1, 0) != 0) return;

        try
        {
            btnCapture.Enabled = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var frame = _camera.GrabFrame();
            if (frame == null)
            {
                lblStatus.Text = "抓帧失败，请检查摄像头";
                return;
            }

            var detections = await _detection.DetectAsync(frame);
            frame.Dispose();

            if (detections.Count == 0)
            {
                lblName.Text = "未检测到物品";
                lblConfidence.Text = "";
                lblStatus.Text = "请调整角度或光照";
                dgvCandidates.DataSource = null;
                return;
            }

            var top = detections[0];
            lblConfidence.Text = $"{top.Confidence:P1}";

            var result = await _matching.ResolveAsync(top.Name, top.Confidence);

            sw.Stop();
            if (result?.Item != null)
            {
                var item = result.Item;
                lblName.Text = item.DisplayName ?? item.Name;
                lblStatus.Text = $"{item.Specification} | ¥{item.Price:F2} | 库存:{item.Stock} | "
                    + $"{sw.ElapsedMilliseconds}ms | 方式:{result.MatchMethod}";
                BindCandidates(result.Candidates);
            }
            else
            {
                lblName.Text = top.Name;
                lblStatus.Text = "未知物品，点此添加";
                dgvCandidates.DataSource = null;
            }

            _logger.LogInformation(
                "Detect: name={Name}, conf={Conf}, method={Method}, elapsed={Elapsed}ms",
                top.Name, top.Confidence, result?.MatchMethod ?? "None", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detection failed");
            MessageBox.Show($"识别失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCapture.Enabled = true;
            Interlocked.Exchange(ref _detecting, 0);
        }
    }

    private void BindCandidates(List<ItemInfo> candidates)
    {
        if (candidates.Count <= 1)
        {
            dgvCandidates.DataSource = null;
            return;
        }

        var dt = new DataTable();
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Specification", typeof(string));
        dt.Columns.Add("Price", typeof(decimal));
        foreach (var c in candidates)
            dt.Rows.Add(c.DisplayName ?? c.Name, c.Specification, c.Price);

        dgvCandidates.DataSource = dt;
        dgvCandidates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private async Task DoFeedbackAsync()
    {
        if (dgvCandidates.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先在候选列表中选择正确的物品", "提示");
            return;
        }

        var selectedName = dgvCandidates.SelectedRows[0].Cells["Name"].Value?.ToString();
        var detectedName = lblName.Text;

        if (!string.IsNullOrEmpty(selectedName) && !string.IsNullOrEmpty(detectedName))
        {
            await _database.AddOrUpdateMappingAsync(detectedName, selectedName);
            lblStatus.Text = $"已记录映射: {detectedName} → {selectedName}";
        }
    }

    private void StartAutoDetect()
    {
        _autoTimer = new System.Windows.Forms.Timer { Interval = AppConfig.AutoDetectIntervalMs };
        _autoTimer.Tick += async (_, _) => await DoDetectAsync();
        _autoTimer.Start();
    }

    private void StopAutoDetect()
    {
        _autoTimer?.Stop();
        _autoTimer?.Dispose();
        _autoTimer = null;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        StopAutoDetect();
        _camera.StopCamera();
        _camera.Dispose();
        _detection.Dispose();
        _database.Dispose();
    }
}
