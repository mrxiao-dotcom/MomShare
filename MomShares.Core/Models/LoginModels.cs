namespace MomShares.Core.Models;

/// <summary>
/// 登录请求模型
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名或手机号
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 登录类型：Admin 或 Holder
    /// </summary>
    public string LoginType { get; set; } = "Admin";
}

/// <summary>
/// 登录响应模型
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户类型：Admin 或 Holder
    /// </summary>
    public string UserType { get; set; } = string.Empty;
    
    /// <summary>
    /// Token过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

