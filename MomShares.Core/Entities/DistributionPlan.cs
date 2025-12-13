namespace MomShares.Core.Entities;

/// <summary>
/// 分配方案实体
/// </summary>
public class DistributionPlan
{
    public int Id { get; set; }
    
    /// <summary>
    /// 产品ID（一个产品一个分配方案）
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// 优先方分配比例（百分比，如30表示30%）
    /// </summary>
    public decimal PriorityRatio { get; set; } = 30m;
    
    /// <summary>
    /// 劣后方分配比例（百分比，如40表示40%）
    /// </summary>
    public decimal SubordinateRatio { get; set; } = 40m;
    
    /// <summary>
    /// 管理方分配比例（百分比，如10表示10%）
    /// </summary>
    public decimal ManagerRatio { get; set; } = 10m;
    
    /// <summary>
    /// 投顾方分配比例（百分比，如20表示20%）
    /// </summary>
    public decimal AdvisorRatio { get; set; } = 20m;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public virtual Product Product { get; set; } = null!;
}

