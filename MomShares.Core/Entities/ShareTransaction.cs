using MomShares.Core.Entities;
using MomShares.Core.Enums;

namespace MomShares.Core.Entities;

/// <summary>
/// 份额操作记录实体
/// </summary>
public class ShareTransaction
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
    /// 操作类型
    /// </summary>
    public ShareTransactionType TransactionType { get; set; }
    
    /// <summary>
    /// 操作日期
    /// </summary>
    public DateTime TransactionDate { get; set; }
    
    /// <summary>
    /// 份额变化（正数=增加，负数=减少）
    /// </summary>
    public decimal ShareChange { get; set; }
    
    /// <summary>
    /// 操作价格
    /// </summary>
    public decimal? TransactionPrice { get; set; }
    
    /// <summary>
    /// 当时净值
    /// </summary>
    public decimal NetValueAtTime { get; set; }
    
    /// <summary>
    /// 交易对方ID（如果是转让）
    /// </summary>
    public int? CounterpartyId { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Holder Holder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Holder? Counterparty { get; set; }
}

