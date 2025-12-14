using MomShares.Core.Interfaces;

namespace MomShares.Infrastructure.Services;

/// <summary>
/// 密码加密服务实现
/// </summary>
public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                Console.WriteLine($"[PasswordService] 验证失败: 密码或哈希为空");
                return false;
            }
            
            // BCrypt 哈希值应该以 $2a$、$2b$ 或 $2y$ 开头
            if (!hashedPassword.StartsWith("$2"))
            {
                Console.WriteLine($"[PasswordService] 警告: 密码哈希格式不正确: {hashedPassword.Substring(0, Math.Min(20, hashedPassword.Length))}...");
            }
            
            var result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            Console.WriteLine($"[PasswordService] 密码验证结果: {result}, 密码长度: {password.Length}, 哈希长度: {hashedPassword.Length}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordService] 密码验证异常: {ex.Message}");
            return false;
        }
    }
}

