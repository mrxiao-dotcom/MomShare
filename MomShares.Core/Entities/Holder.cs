namespace MomShares.Core.Entities;

/// <summary>
/// 持有者实体
/// </summary>
public class Holder
{
    public int Id { get; set; }
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 手机号（登录账号，唯一）
    /// </summary>
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希值
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 联系电话
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// 开户银行
    /// </summary>
    public string? BankName { get; set; }
    
    /// <summary>
    /// 银行卡号
    /// </summary>
    public string? BankAccount { get; set; }
    
    /// <summary>
    /// 户名
    /// </summary>
    public string? AccountName { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ICollection<HolderShare> HolderShares { get; set; } = new List<HolderShare>();
    public virtual ICollection<ShareTransaction> ShareTransactions { get; set; } = new List<ShareTransaction>();
    public virtual ICollection<ShareTransaction> CounterpartyTransactions { get; set; } = new List<ShareTransaction>();
    public virtual ICollection<DividendDetail> DividendDetails { get; set; } = new List<DividendDetail>();
}

