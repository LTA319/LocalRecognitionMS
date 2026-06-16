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

    /// <summary>
    /// 分层匹配：L1 别名映射 → L2 精确+FTS5 → L3 FuzzySharp → L4 语义向量（预留）
    /// </summary>
    public async Task<MatchResult?> ResolveAsync(string detectedName, double confidence)
    {
        var candidates = new List<ItemInfo>();
        var fuzzyThreshold = AppConfig.FuzzyThreshold;
        var topN = AppConfig.SearchTopN;

        // L1: 别名映射表 — 内存 ConcurrentDictionary 查找，<1ms
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

        // L2: 精确匹配 + FTS5 全文搜索，<10ms
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

        // L3: FuzzySharp 字符串模糊匹配 — 遍历全量缓存计算相似度，<100ms
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

        // L4: 语义向量匹配（预留扩展点，默认不启用）
        if (AppConfig.SemanticMatchEnabled)
        {
        }

        return null;
    }
}
