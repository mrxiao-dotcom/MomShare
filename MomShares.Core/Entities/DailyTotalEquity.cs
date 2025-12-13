namespace MomShares.Core.Entities;

/// <summary>
/// 每日总权益记录实体
/// </summary>
public class DailyTotalEquity
{
    public int Id { get; set; }
    
    /// <summary>
    /// 记录日期
    /// </summary>
    public DateTime RecordDate { get; set; }
    
    /// <summary>
    /// 总权益金额（所有产品的当前权益之和）
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}

