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

        // Layer 1: Alias mapping table (in-memory)
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

        // Layer 2: Exact match + FTS5
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

        // Layer 3: FuzzySharp string similarity
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

        // Layer 4: Semantic vector matching (reserved, skipped by default)
        if (AppConfig.SemanticMatchEnabled)
        {
            // Reserved extension point: SemanticMatchAsync(detectedName, allItems)
        }

        return null;
    }
}
