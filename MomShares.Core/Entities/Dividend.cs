using MomShares.Core.Entities;

namespace MomShares.Core.Entities;

/// <summary>
/// 分红记录实体
/// </summary>
public class Dividend
{
    public int Id { get; set; }
    
    /// <summary>
    /// 产品ID
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// 分红日期
    /// </summary>
    public DateTime DividendDate { get; set; }
    
    /// <summary>
    /// 分红总额
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<DividendDetail> DividendDetails { get; set; } = new List<DividendDetail>();
    public virtual ICollection<DividendDistribution> DividendDistributions { get; set; } = new List<DividendDistribution>();
}

