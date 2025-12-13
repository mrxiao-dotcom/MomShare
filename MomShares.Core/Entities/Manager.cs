namespace MomShares.Core.Entities;

/// <summary>
/// 管理方实体
/// </summary>
public class Manager
{
    public int Id { get; set; }
    
    /// <summary>
    /// 管理方名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; }
    
    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<DividendDistribution> DividendDistributions { get; set; } = new List<DividendDistribution>();
}

