namespace Vazer.Core.Modrinth;

/// <summary>
/// Trạng thái phân trang cho Modrinth search (item PH-2/3).
/// </summary>
public sealed class PaginationState
{
    public int Offset { get; set; }
    public int Limit { get; set; } = 20;
    public bool HasMore { get; set; }
    public int Total { get; set; }
    public bool IsLoadingMore { get; set; }

    public void Reset(int limit = 20)
    {
        Offset = 0;
        Limit = Math.Clamp(limit, 1, 100);
        HasMore = false;
        Total = 0;
        IsLoadingMore = false;
    }

    public void Update(int hitsCount, int totalHits)
    {
        Offset += hitsCount;
        Total = totalHits;
        HasMore = Offset < totalHits;
        IsLoadingMore = false;
    }
}
