using MomShares.Core.Entities;

namespace MomShares.Core.Entities;

/// <summary>
/// 产品净值记录实体
/// </summary>
public class ProductNetValue
{
    public int Id { get; set; }
    
    /// <summary>
    /// 产品ID
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// 净值日期
    /// </summary>
    public DateTime NetValueDate { get; set; }
    
    /// <summary>
    /// 净值值
    /// </summary>
    public decimal NetValue { get; set; }
    
    /// <summary>
    /// 录入时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Product Product { get; set; } = null!;
}

