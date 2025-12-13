namespace MomShares.Core.Entities;

/// <summary>
/// 分红分配记录实体（记录管理方和投顾方的分红）
/// </summary>
public class DividendDistribution
{
    public int Id { get; set; }
    
    /// <summary>
    /// 分红ID
    /// </summary>
    public int DividendId { get; set; }
    
    /// <summary>
    /// 分配类型：Manager（管理方）或 Advisor（投顾方）
    /// </summary>
    public string DistributionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 管理方ID（如果分配类型是Manager）
    /// </summary>
    public int? ManagerId { get; set; }
    
    /// <summary>
    /// 投顾方ID（如果分配类型是Advisor）
    /// </summary>
    public int? AdvisorId { get; set; }
    
    /// <summary>
    /// 分配金额
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// 分配比例（分红时使用的比例）
    /// </summary>
    public decimal Ratio { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Dividend Dividend { get; set; } = null!;
    public virtual Manager? Manager { get; set; }
    public virtual Advisor? Advisor { get; set; }
}

