namespace MomShares.Core.Entities;

/// <summary>
/// 产品实体
/// </summary>
public class Product
{
    public int Id { get; set; }
    
    /// <summary>
    /// 产品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 产品代码（可选）
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// 当前净值
    /// </summary>
    public decimal CurrentNetValue { get; set; } = 1.0m;
    
    /// <summary>
    /// 总份额数量
    /// </summary>
    public decimal TotalShares { get; set; } = 0;
    
    /// <summary>
    /// 总金额
    /// </summary>
    public decimal TotalAmount { get; set; } = 0;
    
    /// <summary>
    /// 初始权益（初始总金额）
    /// </summary>
    public decimal InitialAmount { get; set; } = 0;
    
    /// <summary>
    /// 分配方案ID
    /// </summary>
    public int DistributionPlanId { get; set; }
    
    /// <summary>
    /// 投顾ID
    /// </summary>
    public int? AdvisorId { get; set; }
    
    /// <summary>
    /// 管理方ID
    /// </summary>
    public int? ManagerId { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public virtual DistributionPlan DistributionPlan { get; set; } = null!;
    public virtual Advisor? Advisor { get; set; }
    public virtual Manager? Manager { get; set; }
    public virtual ICollection<ProductNetValue> NetValues { get; set; } = new List<ProductNetValue>();
    public virtual ICollection<HolderShare> HolderShares { get; set; } = new List<HolderShare>();
    public virtual ICollection<ShareTransaction> ShareTransactions { get; set; } = new List<ShareTransaction>();
    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();
    public virtual ICollection<CapitalIncrease> CapitalIncreases { get; set; } = new List<CapitalIncrease>();
}

