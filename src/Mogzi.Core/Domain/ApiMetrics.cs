namespace Mogzi.Domain;

public class ApiMetrics
{
    public int TotalTokensIn { get; set; } = 0;
    public int TotalTokensOut { get; set; } = 0;
    public int? TotalCacheWrites { get; set; }
    public int? TotalCacheReads { get; set; }
    public double TotalTotalCost { get; set; } = 0;
}