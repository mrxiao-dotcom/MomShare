using Microsoft.EntityFrameworkCore;
using MomShares.Core.Entities;
using MomShares.Core.Interfaces;
using MomShares.Infrastructure.Data;

namespace MomShares.Infrastructure.Services;

/// <summary>
/// 数据库初始化服务
/// </summary>
public class DatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public DatabaseInitializer(ApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        // 确保数据库已创建
        await _context.Database.EnsureCreatedAsync();

        // 检查是否已有管理员
        if (!await _context.Admins.AnyAsync())
        {
            // 创建默认管理员
            var defaultAdmin = new Admin
            {
                Username = "admin",
                PasswordHash = _passwordService.HashPassword("admin123"),
                CreatedAt = DateTime.Now
            };

            _context.Admins.Add(defaultAdmin);
            await _context.SaveChangesAsync();
        }
    }
}

