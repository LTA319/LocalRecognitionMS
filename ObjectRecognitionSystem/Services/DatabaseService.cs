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
        // 启动时全量加载映射表和物品表到内存，后续 L1/L3 匹配无需访问数据库
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

        // FTS5 MATCH 不支持参数化，使用 FromSqlRaw 拼接；keyword + "*" 启用前缀匹配
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
