using MomShares.Core.Entities;

namespace MomShares.Core.Entities;

/// <summary>
/// 持有者份额实体
/// </summary>
public class HolderShare
{
    public int Id { get; set; }
    
    /// <summary>
    /// 持有者ID
    /// </summary>
    public int HolderId { get; set; }
    
    /// <summary>
    /// 产品ID
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// 份额数量
    /// </summary>
    public decimal ShareAmount { get; set; } = 0;
    
    /// <summary>
    /// 出资金额
    /// </summary>
    public decimal InvestmentAmount { get; set; } = 0;
    
    /// <summary>
    /// 份额类型：Priority（优先方）或 Subordinate（劣后方），默认为劣后方
    /// </summary>
    public string ShareType { get; set; } = "Subordinate";
    
    /// <summary>
    /// 关联时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public virtual Holder Holder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

