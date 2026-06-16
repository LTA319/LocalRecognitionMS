# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

基于 .NET 8 WinForms 的本地物料识别桌面应用 — 摄像头实时预览，YOLO ONNX 模型推理，本地 SQLite + 分层匹配引擎将检测结果映射到物料数据库。

## 构建与运行

```bash
cd ObjectRecognitionSystem
dotnet build              # Debug
dotnet run                # 启动 (需要摄像头 + Assets/models/yolo11n.onnx)
```

项目为单解决方案 `ObjectRecognitionSystem.sln`，无测试项目，无 CI/CD。

## 技术栈

| 组件 | 库 | 关键 API |
|------|-----|---------|
| 目标检测 | YoloDotNet 2.1.0 | `_yolo.RunObjectDetection(SKImage, confidence, iou)` 输入为 **SkiaSharp SKImage**，非 Bitmap |
| 摄像头 | Emgu.CV 4.9 + Emgu.CV.Bitmap | `frame.ToImage<Bgr, byte>().ToBitmap()` — 两步转换，`Bgr` 在 `Emgu.CV.Structure` 命名空间 |
| 字符串匹配 | FuzzySharp 2.0.2 | `Fuzz.WeightedRatio()`, `Fuzz.PartialRatio()` 返回 0-100 整数 |
| 数据库 | EF Core 8.0 + SQLite | FTS5 虚拟表 (`ItemsFTS`) 通过 `FromSqlRaw` 查询 |

## 架构

三层分层：**Forms (UI) → Services → Data**

```
MainForm (Forms/)
  ├── CameraService      — Emgu.CV 摄像头预览 + 帧抓取
  ├── DetectionService   — YoloDotNet ONNX 推理
  ├── MatchingService    — 分层匹配引擎 (L1→L2→L3→L4)
  └── DatabaseService    — SQLite CRUD + 内存缓存 (ConcurrentDictionary)
        └── AppDbContext  — EF Core with FTS5
```

服务在 `MainForm` 构造函数中手动创建（无 DI 容器），MatchingService 依赖 DatabaseService。

## 匹配流水线

4 层级联，命中即返回：

| 层 | 方法 | 速度 | 关键类/方法 |
|---|------|------|------------|
| L1 | 别名映射表 | <1ms | `DatabaseService.GetMappedName()` 查内存 `ConcurrentDictionary` |
| L2 | 精确 + FTS5 | <10ms | `GetByNameAsync()` + `SearchFtsAsync()` (SQL MATCH) |
| L3 | FuzzySharp | <100ms | 遍历全量缓存 `GetAllCachedItems()`, 阈值默认 70 |
| L4 | 语义向量 | 预留 | `SemanticMatchEnabled=false`, 未实现 |

L1 映射表支持运行时扩充：用户在候选列表中选择 → `AddOrUpdateMappingAsync()` → 写入 DB + 刷新内存缓存。

## 启动顺序

```
Program.Main()
  → 验证 ONNX 模型文件存在
  → 创建临时 DetectionService + WarmUp() (空图 640×640, 触发 ONNX JIT)
  → 销毁临时实例
  → Application.Run(new MainForm())
       → 构造: EnsureCreated() + LoadCacheAsync() (同步等待)
       → 重新创建 DetectionService (第二次加载模型)
```

注意：模型被加载两次 — Program.cs 为了 fail-fast 预检，MainForm 再加载一次实际使用。

## 配置

`App.config` 由 `ConfigurationManager.AppSettings` 读取，`AppConfig.cs` 提供静态类型化访问。共 9 个配置键：`ConnectionString`, `OnnxModelPath`, `ConfidenceThreshold`(0.5), `CudaEnabled`(false), `AutoDetectIntervalMs`(1500), `PreviewFps`(30), `SemanticMatchEnabled`(false), `FuzzyThreshold`(70), `SearchTopN`(5)。

## 线程模型

- 摄像头预览: `System.Threading.Timer` (线程池回调) → 需用旧 `Image.Dispose()` 防 GDI+ 泄漏
- 推理: `Task.Run` 包装同步 `RunObjectDetection`，不阻塞 UI
- 防重入: `Interlocked.CompareExchange` 保护 `DoDetectAsync()`
- 缓存: `ConcurrentDictionary` 读多写少

## 已知限制

- 无测试覆盖
- 语义向量匹配 (L4) 未实现
- `ItemsFTS` 虚拟表通过 raw SQL 创建 (`EnsureFts5Created`)，非 EF 迁移 — 首次运行自动建表
- 摄像头默认 index=0，不支持切换摄像头
- CUDA 需 NVIDIA GPU + CUDA 运行时，默认关闭
