namespace MomShares.Core.Interfaces;

/// <summary>
/// JWT Token服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 生成Token
    /// </summary>
    string GenerateToken(int userId, string username, string userType);
    
    /// <summary>
    /// 验证Token
    /// </summary>
    bool ValidateToken(string token);
}

