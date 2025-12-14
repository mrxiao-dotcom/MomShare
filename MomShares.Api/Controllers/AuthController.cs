using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Core.Interfaces;
using MomShares.Core.Models;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordService _passwordService;

    public AuthController(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordService passwordService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 管理员登录
    /// </summary>
    [HttpPost("admin/login")]
    public async Task<ActionResult<LoginResponse>> AdminLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "用户名和密码不能为空" });
        }

        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Username == request.Username);

        if (admin == null || !_passwordService.VerifyPassword(request.Password, admin.PasswordHash))
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var token = _jwtTokenService.GenerateToken(admin.Id, admin.Username, "Admin");
        var expirationMinutes = int.Parse(Request.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["JwtSettings:ExpirationMinutes"] ?? "1440");

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = admin.Id,
            Username = admin.Username,
            UserType = "Admin",
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });
    }

    /// <summary>
    /// 持有者登录
    /// </summary>
    [HttpPost("holder/login")]
    public async Task<ActionResult<LoginResponse>> HolderLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "手机号和密码不能为空" });
        }

        var holder = await _context.Holders
            .FirstOrDefaultAsync(h => h.Phone == request.Username);

        if (holder == null)
        {
            Console.WriteLine($"[HolderLogin] 持有人不存在: {request.Username}");
            return Unauthorized(new { message = "手机号或密码错误" });
        }

        // 检查密码哈希是否为空（可能是旧数据）
        if (string.IsNullOrWhiteSpace(holder.PasswordHash))
        {
            Console.WriteLine($"[HolderLogin] 持有人密码哈希为空: {holder.Id}");
            return Unauthorized(new { message = "密码未设置，请联系管理员" });
        }

        // 验证密码
        var isPasswordValid = _passwordService.VerifyPassword(request.Password, holder.PasswordHash);
        Console.WriteLine($"[HolderLogin] 持有人ID: {holder.Id}, 手机号: {request.Username}, 密码验证: {isPasswordValid}");
        
        if (!isPasswordValid)
        {
            Console.WriteLine($"[HolderLogin] 密码验证失败，输入的密码长度: {request.Password?.Length ?? 0}");
            return Unauthorized(new { message = "手机号或密码错误" });
        }

        var token = _jwtTokenService.GenerateToken(holder.Id, holder.Phone, "Holder");
        var expirationMinutes = int.Parse(Request.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["JwtSettings:ExpirationMinutes"] ?? "1440");

        Console.WriteLine($"[HolderLogin] 登录成功: {holder.Name} (ID: {holder.Id})");

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = holder.Id,
            Username = holder.Name,
            UserType = "Holder",
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        });
    }
}

