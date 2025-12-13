using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MomShares.Core.Interfaces;
using MomShares.Infrastructure.Data;
using MomShares.Infrastructure.Services;

namespace MomShares.Infrastructure;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加基础设施服务
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // 注册服务
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }
}

