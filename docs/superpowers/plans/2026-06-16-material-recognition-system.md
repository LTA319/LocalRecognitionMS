# 物料识别系统实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 基于概要设计说明书，从零搭建 .NET 8 WinForms 物料识别系统，包含摄像头预览、YOLO ONNX 推理、SQLite 分层匹配引擎。

**Architecture:** 分层架构 — 表示层 (MainForm) → 服务层 (Camera/Detection/Matching/Database) → 数据层 (EF Core + SQLite)。所有耗时操作异步化，UI 线程只负责绑定。

**Tech Stack:** .NET 8 WinForms, YoloDotNet, Emgu.CV, EF Core + SQLite, FuzzySharp, System.Configuration

---

### Task 1: 项目脚手架与基础配置

**Files:**
- Create: `ObjectRecognitionSystem/ObjectRecognitionSystem.csproj`
- Create: `ObjectRecognitionSystem/App.config`
- Create: `ObjectRecognitionSystem/AppConfig.cs`
- Create: `ObjectRecognitionSystem/Program.cs` (骨架)

- [ ] **Step 1: 创建项目文件**

```xml
<!-- ObjectRecognitionSystem/ObjectRecognitionSystem.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="YoloDotNet" Version="2.1.0" />
    <PackageReference Include="Emgu.CV.runtime.windows" Version="4.9.0.5494" />
    <PackageReference Include="Emgu.CV" Version="4.9.0.5494" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
    <PackageReference Include="FuzzySharp" Version="2.0.2" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Assets\models\yolo11n.onnx">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="App.config">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 创建 App.config**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="ConnectionString" value="Data Source=items.db" />
    <add key="OnnxModelPath" value="Assets/models/yolo11n.onnx" />
    <add key="ConfidenceThreshold" value="0.5" />
    <add key="CudaEnabled" value="false" />
    <add key="AutoDetectIntervalMs" value="1500" />
    <add key="PreviewFps" value="30" />
    <add key="SemanticMatchEnabled" value="false" />
    <add key="FuzzyThreshold" value="70" />
    <add key="SearchTopN" value="5" />
  </appSettings>
</configuration>
```

- [ ] **Step 3: 创建 AppConfig 静态配置类**

```csharp
// ObjectRecognitionSystem/AppConfig.cs
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
```

- [ ] **Step 4: 创建 Program.cs 骨架**

```csharp
// ObjectRecognitionSystem/Program.cs
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
```

- [ ] **Step 5: 还原包并构建**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet restore
dotnet build
```

预期: Build succeeded.

---

### Task 2: 数据模型

**Files:**
- Create: `ObjectRecognitionSystem/Models/ItemInfo.cs`
- Create: `ObjectRecognitionSystem/Models/NameMapping.cs`
- Create: `ObjectRecognitionSystem/Models/DetectionResult.cs`
- Create: `ObjectRecognitionSystem/Models/MatchResult.cs`

- [ ] **Step 1: 创建 ItemInfo 实体**

```csharp
// ObjectRecognitionSystem/Models/ItemInfo.cs
namespace ObjectRecognitionSystem.Models;

public class ItemInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Code { get; set; }
    public string? Category { get; set; }
    public string? Specification { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
}
```

- [ ] **Step 2: 创建 NameMapping 实体**

```csharp
// ObjectRecognitionSystem/Models/NameMapping.cs
namespace ObjectRecognitionSystem.Models;

public class NameMapping
{
    public int Id { get; set; }
    public string DetectedName { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string? Category { get; set; }
}
```

- [ ] **Step 3: 创建 DetectionResult 模型**

```csharp
// ObjectRecognitionSystem/Models/DetectionResult.cs
namespace ObjectRecognitionSystem.Models;

public class DetectionResult
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Rectangle BoundingBox { get; set; }
}
```

- [ ] **Step 4: 创建 MatchResult 模型**

```csharp
// ObjectRecognitionSystem/Models/MatchResult.cs
namespace ObjectRecognitionSystem.Models;

public class MatchResult
{
    public ItemInfo? Item { get; set; }
    public string MatchMethod { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public List<ItemInfo> Candidates { get; set; } = new();
}
```

- [ ] **Step 5: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 3: 数据层 — DbContext 与 FTS5

**Files:**
- Create: `ObjectRecognitionSystem/Data/AppDbContext.cs`

- [ ] **Step 1: 创建 AppDbContext**

```csharp
// ObjectRecognitionSystem/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ObjectRecognitionSystem.Models;

namespace ObjectRecognitionSystem.Data;

public class AppDbContext : DbContext
{
    public DbSet<ItemInfo> Items { get; set; } = null!;
    public DbSet<NameMapping> NameMappings { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(AppConfig.ConnectionString);

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ItemInfo>(e =>
        {
            e.ToTable("Items");
            e.HasIndex(i => i.Name);
            e.HasIndex(i => i.Code);
        });

        model.Entity<NameMapping>(e =>
        {
            e.ToTable("NameMappings");
            e.HasIndex(m => m.DetectedName);
        });
    }

    /// <summary>创建 FTS5 虚拟表和触发器，确保数据库新建时也有全文搜索能力。</summary>
    public void EnsureFts5Created()
    {
        Database.ExecuteSqlRaw(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS ItemsFTS USING fts5(
                Name, Code, Specification, DisplayName,
                content='Items', content_rowid='Id'
            );

            CREATE TRIGGER IF NOT EXISTS items_ai AFTER INSERT ON Items BEGIN
                INSERT INTO ItemsFTS(rowid, Name, Code, Specification, DisplayName)
                VALUES (new.Id, new.Name, new.Code, new.Specification, new.DisplayName);
            END;

            CREATE TRIGGER IF NOT EXISTS items_ad AFTER DELETE ON Items BEGIN
                INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Code, Specification, DisplayName)
                VALUES ('delete', old.Id, old.Name, old.Code, old.Specification, old.DisplayName);
            END;

            CREATE TRIGGER IF NOT EXISTS items_au AFTER UPDATE ON Items BEGIN
                INSERT INTO ItemsFTS(ItemsFTS, rowid, Name, Code, Specification, DisplayName)
                VALUES ('delete', old.Id, old.Name, old.Code, old.Specification, old.DisplayName);
                INSERT INTO ItemsFTS(rowid, Name, Code, Specification, DisplayName)
                VALUES (new.Id, new.Name, new.Code, new.Specification, new.DisplayName);
            END;
        ");
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 4: DatabaseService

**Files:**
- Create: `ObjectRecognitionSystem/Services/DatabaseService.cs`

- [ ] **Step 1: 创建 DatabaseService**

```csharp
// ObjectRecognitionSystem/Services/DatabaseService.cs
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ObjectRecognitionSystem.Data;
using ObjectRecognitionSystem.Models;

namespace ObjectRecognitionSystem.Services;

public class DatabaseService : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ConcurrentDictionary<string, string> _mappingCache = new();
    private readonly ConcurrentDictionary<string, ItemInfo> _itemCache = new();
    private List<ItemInfo> _allItems = new();

    public DatabaseService()
    {
        _context = new AppDbContext();
    }

    public void EnsureCreated()
    {
        _context.Database.EnsureCreated();
        _context.EnsureFts5Created();
    }

    public async Task LoadCacheAsync()
    {
        var mappings = await _context.NameMappings.ToListAsync();
        foreach (var m in mappings)
            _mappingCache[m.DetectedName] = m.StandardName;

        _allItems = await _context.Items.ToListAsync();
        foreach (var item in _allItems)
            _itemCache[item.Name] = item;
    }

    public string? GetMappedName(string detectedName)
    {
        _mappingCache.TryGetValue(detectedName, out var standard);
        return standard;
    }

    public ItemInfo? GetCachedItem(string name)
    {
        _itemCache.TryGetValue(name, out var item);
        return item;
    }

    public List<ItemInfo> GetAllCachedItems() => _allItems;

    public async Task<ItemInfo?> GetByNameAsync(string name)
    {
        return await _context.Items
            .FirstOrDefaultAsync(i => i.Name == name || i.DisplayName == name);
    }

    public async Task<List<ItemInfo>> SearchFtsAsync(string keyword, int top = 5)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<ItemInfo>();

        return await _context.Items
            .FromSqlRaw(@"
                SELECT i.* FROM Items i
                INNER JOIN ItemsFTS fts ON i.Id = fts.rowid
                WHERE ItemsFTS MATCH {0}
                ORDER BY rank
                LIMIT {1}",
                keyword + "*", top)
            .ToListAsync();
    }

    public async Task<List<ItemInfo>> FuzzySearchAsync(string keyword, int top = 10)
    {
        return await _context.Items
            .Where(i => EF.Functions.Like(i.Name, $"%{keyword}%")
                     || EF.Functions.Like(i.Code ?? "", $"%{keyword}%")
                     || EF.Functions.Like(i.Specification ?? "", $"%{keyword}%"))
            .OrderBy(i => i.Name)
            .Take(top)
            .ToListAsync();
    }

    public async Task AddOrUpdateMappingAsync(string detectedName, string standardName)
    {
        var existing = await _context.NameMappings
            .FirstOrDefaultAsync(m => m.DetectedName == detectedName);

        if (existing != null)
            existing.StandardName = standardName;
        else
            _context.NameMappings.Add(new NameMapping
            {
                DetectedName = detectedName,
                StandardName = standardName
            });

        await _context.SaveChangesAsync();
        _mappingCache[detectedName] = standardName;
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 5: CameraService

**Files:**
- Create: `ObjectRecognitionSystem/Services/CameraService.cs`

- [ ] **Step 1: 创建 CameraService**

```csharp
// ObjectRecognitionSystem/Services/CameraService.cs
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;

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

                var oldImage = _previewBox.Image;
                _previewBox.Image = frame.ToBitmap();
                oldImage?.Dispose();
            }
            catch
            {
                // 丢帧时忽略
            }
        }, null, 0, interval);
    }

    public Bitmap? GrabFrame()
    {
        if (_capture == null) return null;

        using var frame = _capture.QueryFrame();
        return frame?.ToBitmap();
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
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 6: DetectionService

**Files:**
- Create: `ObjectRecognitionSystem/Services/DetectionService.cs`

- [ ] **Step 1: 创建 DetectionService**

```csharp
// ObjectRecognitionSystem/Services/DetectionService.cs
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
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 7: MatchingService — 分层匹配引擎

**Files:**
- Create: `ObjectRecognitionSystem/Services/MatchingService.cs`

- [ ] **Step 1: 创建 MatchingService**

```csharp
// ObjectRecognitionSystem/Services/MatchingService.cs
using System.Collections.Concurrent;
using FuzzySharp;
using ObjectRecognitionSystem.Models;

namespace ObjectRecognitionSystem.Services;

public class MatchingService
{
    private readonly DatabaseService _db;

    public MatchingService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<MatchResult?> ResolveAsync(string detectedName, double confidence)
    {
        var candidates = new List<ItemInfo>();
        var fuzzyThreshold = AppConfig.FuzzyThreshold;
        var topN = AppConfig.SearchTopN;

        // Layer 1: 别名映射表
        var mappedName = _db.GetMappedName(detectedName);
        if (mappedName != null)
        {
            var item = _db.GetCachedItem(mappedName)
                    ?? await _db.GetByNameAsync(mappedName);
            if (item != null)
            {
                return new MatchResult
                {
                    Item = item,
                    MatchMethod = "Mapping",
                    MatchScore = 1.0,
                    Candidates = new List<ItemInfo> { item }
                };
            }
        }

        // Layer 2: 精确匹配 + FTS5
        var exact = await _db.GetByNameAsync(detectedName);
        if (exact != null)
        {
            candidates.Add(exact);
        }
        var ftsResults = await _db.SearchFtsAsync(detectedName, topN);
        candidates = candidates.Union(ftsResults).Distinct().ToList();
        if (candidates.Count > 0)
        {
            return new MatchResult
            {
                Item = candidates.First(),
                MatchMethod = exact != null ? "Exact" : "FTS5",
                MatchScore = exact != null ? 1.0 : 0.9,
                Candidates = candidates
            };
        }

        // Layer 3: FuzzySharp 模糊匹配
        var allItems = _db.GetAllCachedItems();
        var fuzzyMatches = allItems
            .Select(item => new
            {
                Item = item,
                Score = Math.Max(
                    Fuzz.WeightedRatio(detectedName, item.Name),
                    Fuzz.PartialRatio(detectedName, item.Name))
            })
            .Where(m => m.Score >= fuzzyThreshold)
            .OrderByDescending(m => m.Score)
            .Take(topN)
            .ToList();

        if (fuzzyMatches.Any())
        {
            return new MatchResult
            {
                Item = fuzzyMatches.First().Item,
                MatchMethod = "Fuzzy",
                MatchScore = fuzzyMatches.First().Score,
                Candidates = fuzzyMatches.Select(m => m.Item).ToList()
            };
        }

        // Layer 4: 向量语义匹配（占位，默认跳过）
        if (AppConfig.SemanticMatchEnabled)
        {
            // 预留扩展点: SemanticMatchAsync(detectedName, allItems)
        }

        return null; // 完全未匹配，由 UI 提示用户
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 8: MainForm — 主界面

**Files:**
- Create: `ObjectRecognitionSystem/Forms/MainForm.cs`
- Create: `ObjectRecognitionSystem/Forms/MainForm.Designer.cs`

- [ ] **Step 1: 创建 MainForm（代码隐藏）**

```csharp
// ObjectRecognitionSystem/Forms/MainForm.cs
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
    private int _detecting; // 0 = idle, 1 = busy（Interlocked 防重入）

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
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: 编译失败 — 缺少 Designer.cs 文件，Task 9 补充。

---

### Task 9: MainForm.Designer.cs — UI 布局

**Files:**
- Create: `ObjectRecognitionSystem/Forms/MainForm.Designer.cs`

- [ ] **Step 1: 创建 Designer 文件**

```csharp
// ObjectRecognitionSystem/Forms/MainForm.Designer.cs
namespace ObjectRecognitionSystem.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox pbPreview = null!;
    private Button btnCapture = null!;
    private CheckBox cbAutoDetect = null!;
    private Label lblName = null!;
    private Label lblConfidence = null!;
    private Label lblStatus = null!;
    private DataGridView dgvCandidates = null!;
    private Button btnFeedback = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.pbPreview = new PictureBox();
        this.btnCapture = new Button();
        this.cbAutoDetect = new CheckBox();
        this.lblName = new Label();
        this.lblConfidence = new Label();
        this.lblStatus = new Label();
        this.dgvCandidates = new DataGridView();
        this.btnFeedback = new Button();
        ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).BeginInit();
        this.SuspendLayout();

        // pbPreview
        this.pbPreview.BorderStyle = BorderStyle.FixedSingle;
        this.pbPreview.Location = new Point(12, 12);
        this.pbPreview.Size = new Size(640, 480);
        this.pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
        this.pbPreview.TabIndex = 0;
        this.pbPreview.TabStop = false;

        // btnCapture
        this.btnCapture.Location = new Point(12, 500);
        this.btnCapture.Size = new Size(100, 36);
        this.btnCapture.TabIndex = 1;
        this.btnCapture.Text = "识别";

        // cbAutoDetect
        this.cbAutoDetect.AutoSize = true;
        this.cbAutoDetect.Location = new Point(120, 508);
        this.cbAutoDetect.Size = new Size(90, 21);
        this.cbAutoDetect.TabIndex = 2;
        this.cbAutoDetect.Text = "自动识别";
        this.cbAutoDetect.UseVisualStyleBackColor = true;

        // lblName
        this.lblName.AutoSize = true;
        this.lblName.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
        this.lblName.Location = new Point(670, 20);
        this.lblName.Size = new Size(120, 25);
        this.lblName.TabIndex = 3;
        this.lblName.Text = "物品名称";

        // lblConfidence
        this.lblConfidence.AutoSize = true;
        this.lblConfidence.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this.lblConfidence.Location = new Point(670, 55);
        this.lblConfidence.Size = new Size(50, 20);
        this.lblConfidence.TabIndex = 4;

        // lblStatus
        this.lblStatus.AutoSize = true;
        this.lblStatus.ForeColor = Color.Gray;
        this.lblStatus.Location = new Point(670, 85);
        this.lblStatus.Size = new Size(200, 17);
        this.lblStatus.TabIndex = 5;

        // dgvCandidates
        this.dgvCandidates.AllowUserToAddRows = false;
        this.dgvCandidates.AllowUserToDeleteRows = false;
        this.dgvCandidates.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvCandidates.Location = new Point(670, 120);
        this.dgvCandidates.Size = new Size(400, 200);
        this.dgvCandidates.TabIndex = 6;
        this.dgvCandidates.ReadOnly = true;
        this.dgvCandidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvCandidates.MultiSelect = false;

        // btnFeedback
        this.btnFeedback.Location = new Point(670, 330);
        this.btnFeedback.Size = new Size(120, 30);
        this.btnFeedback.TabIndex = 7;
        this.btnFeedback.Text = "纠正映射";

        // MainForm
        this.AutoScaleDimensions = new SizeF(7F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1090, 550);
        this.Controls.Add(this.btnFeedback);
        this.Controls.Add(this.dgvCandidates);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.lblConfidence);
        this.Controls.Add(this.lblName);
        this.Controls.Add(this.cbAutoDetect);
        this.Controls.Add(this.btnCapture);
        this.Controls.Add(this.pbPreview);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "MainForm";
        this.Text = "物料识别系统";
        ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 10: Program.cs — 启动顺序与预热

**Files:**
- Modify: `ObjectRecognitionSystem/Program.cs`

- [ ] **Step 1: 更新 Program.cs 添加启动顺序与异常保护**

```csharp
// ObjectRecognitionSystem/Program.cs
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
            // 1. 验证配置
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfig.OnnxModelPath);
            if (!File.Exists(modelPath))
            {
                MessageBox.Show($"ONNX 模型文件未找到: {modelPath}\n请将模型文件放入 Assets/models/ 目录。",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. 预热检测服务
            DetectionService? detection = null;
            try
            {
                detection = new DetectionService();
                detection.WarmUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"模型加载失败: {ex.Message}",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                detection?.Dispose();
            }

            // 3. 启动主窗体（内部完成 DB 初始化）
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"系统启动失败: {ex.Message}",
                "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
```

- [ ] **Step 2: 构建验证**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build
```

预期: Build succeeded.

---

### Task 11: 最终构建与完整性检查

- [ ] **Step 1: 完整构建**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet build --configuration Release
```

预期: Build succeeded with 0 errors, 0 warnings.

- [ ] **Step 2: 检查输出目录结构**

```bash
ls -R D:/work/MES/AiXinhdMS/ObjectRecognitionSystem/
```

预期包含:
```
ObjectRecognitionSystem/
├── Forms/
│   ├── MainForm.cs
│   └── MainForm.Designer.cs
├── Services/
│   ├── CameraService.cs
│   ├── DetectionService.cs
│   ├── MatchingService.cs
│   └── DatabaseService.cs
├── Models/
│   ├── ItemInfo.cs
│   ├── NameMapping.cs
│   ├── DetectionResult.cs
│   └── MatchResult.cs
├── Data/
│   └── AppDbContext.cs
├── Assets/models/  (需手动放入 yolo11n.onnx)
├── App.config
├── AppConfig.cs
├── Program.cs
└── ObjectRecognitionSystem.csproj
```

---

### Task 12: 运行验证（需要摄像头 + ONNX 模型）

- [ ] **Step 1: 下载 ONNX 模型**（如未下载）

手动从 Ultralytics 下载 `yolo11n.onnx` 或 `yolov8n.onnx`，放入 `Assets/models/` 目录。

- [ ] **Step 2: 运行应用**

```bash
cd D:/work/MES/AiXinhdMS/ObjectRecognitionSystem
dotnet run
```

预期: 窗体打开，摄像头预览正常，点击"识别"后显示检测结果。

- [ ] **Step 3: 手动验证清单**
  - [ ] 摄像头预览流畅通（≥25 FPS）
  - [ ] 手动点击"识别"按钮，结果区域显示物品名称和置信度
  - [ ] 勾选"自动识别"后定时抓帧
  - [ ] 候选列表显示多个匹配项
  - [ ] 选择候选后点击"纠正映射"，下次同一检测名直接命中
  - [ ] 关闭窗体无异常退出

---

### Task 13: 可选扩展 — 向量语义匹配（L4）

**Files:**
- Modify: `ObjectRecognitionSystem/Services/MatchingService.cs`

说明：此任务仅在 `AppConfig.SemanticMatchEnabled = true` 时生效，需要额外引入 Embedding ONNX 模型（如 `all-MiniLM-L6-v2`）。当前 L1-L3 已覆盖绝大多数场景，L4 为实验性预留。

- [ ] **Step 1: 不实现此任务。** 待 L1-L3 上线后根据实际匹配率数据决定是否投资。

---

*实施计划结束。*
