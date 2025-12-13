using MomShares.Core.Entities;

namespace MomShares.Core.Entities;

/// <summary>
/// 分红明细实体
/// </summary>
public class DividendDetail
{
    public int Id { get; set; }
    
    /// <summary>
    /// 分红ID
    /// </summary>
    public int DividendId { get; set; }
    
    /// <summary>
    /// 持有者ID
    /// </summary>
    public int HolderId { get; set; }
    
    /// <summary>
    /// 分红金额
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// 分红时的份额占比
    /// </summary>
    public decimal ShareRatio { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Dividend Dividend { get; set; } = null!;
    public virtual Holder Holder { get; set; } = null!;
}

