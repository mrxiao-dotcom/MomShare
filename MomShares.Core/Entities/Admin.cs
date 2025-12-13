namespace MomShares.Core.Entities;

/// <summary>
/// 管理员实体
/// </summary>
public class Admin
{
    public int Id { get; set; }
    
    /// <summary>
    /// 用户名（唯一）
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希值
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

