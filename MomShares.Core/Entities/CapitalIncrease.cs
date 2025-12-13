using MomShares.Core.Entities;

namespace MomShares.Core.Entities;

/// <summary>
/// 增资记录实体
/// </summary>
public class CapitalIncrease
{
    public int Id { get; set; }
    
    /// <summary>
    /// 产品ID
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// 增减资日期
    /// </summary>
    public DateTime IncreaseDate { get; set; }
    
    /// <summary>
    /// 操作前总金额（当前/初始权益）
    /// </summary>
    public decimal AmountBefore { get; set; }
    
    /// <summary>
    /// 增减资金额（正数）
    /// </summary>
    public decimal IncreaseAmount { get; set; }
    
    /// <summary>
    /// 操作后总金额（新的初始/当前权益）
    /// </summary>
    public decimal AmountAfter { get; set; }
    
    /// <summary>
    /// 类型：Increase / Decrease
    /// </summary>
    public string Type { get; set; } = "Increase";
    
    /// <summary>
    /// 明细（JSON 存储持有人份额调整）
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Product Product { get; set; } = null!;
}

