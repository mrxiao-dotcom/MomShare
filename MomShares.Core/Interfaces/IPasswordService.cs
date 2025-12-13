namespace MomShares.Core.Interfaces;

/// <summary>
/// 密码加密服务接口
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// 加密密码
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// 验证密码
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);
}

