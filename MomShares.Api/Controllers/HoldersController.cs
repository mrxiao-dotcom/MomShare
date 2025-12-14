using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Core.Interfaces;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 持有者管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class HoldersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public HoldersController(ApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 获取持有者列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Holder>>> GetHolders()
    {
        return await _context.Holders
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取持有者详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Holder>> GetHolder(int id)
    {
        var holder = await _context.Holders
            .Include(h => h.HolderShares)
                .ThenInclude(hs => hs.Product)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        return holder;
    }

    /// <summary>
    /// 创建持有者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Holder>> CreateHolder([FromBody] CreateHolderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "姓名不能为空" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new { message = "手机号不能为空" });
        }

        // 检查手机号是否已存在
        if (await _context.Holders.AnyAsync(h => h.Phone == request.Phone))
        {
            return BadRequest(new { message = "该手机号已被使用" });
        }

        var holder = new Holder
        {
            Name = request.Name,
            Phone = request.Phone,
            PasswordHash = _passwordService.HashPassword(request.Password ?? "888888"),
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            BankName = request.BankName,
            BankAccount = request.BankAccount,
            AccountName = request.AccountName,
            CreatedAt = DateTime.Now
        };

        _context.Holders.Add(holder);
        await _context.SaveChangesAsync();

        // 清除密码哈希值（不返回给客户端）
        holder.PasswordHash = string.Empty;

        return CreatedAtAction(nameof(GetHolder), new { id = holder.Id }, holder);
    }

    /// <summary>
    /// 更新持有者信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHolder(int id, [FromBody] UpdateHolderRequest request)
    {
        var holder = await _context.Holders.FindAsync(id);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "姓名不能为空" });
        }

        holder.Name = request.Name;
        holder.PhoneNumber = request.PhoneNumber;
        holder.Email = request.Email;
        holder.BankName = request.BankName;
        holder.BankAccount = request.BankAccount;
        holder.AccountName = request.AccountName;
        holder.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 修改持有者密码
    /// </summary>
    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        var holder = await _context.Holders.FindAsync(id);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "新密码不能为空" });
        }

        // 记录修改前的信息（用于调试）
        Console.WriteLine($"[ChangePassword] 修改持有人密码: ID={id}, 手机号={holder.Phone}, 新密码长度={request.NewPassword.Length}");

        // 生成新的密码哈希
        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        Console.WriteLine($"[ChangePassword] 新密码哈希长度: {newPasswordHash?.Length ?? 0}");
        
        // 验证新密码哈希是否有效（可以立即验证）
        var verifyTest = _passwordService.VerifyPassword(request.NewPassword, newPasswordHash);
        Console.WriteLine($"[ChangePassword] 密码哈希验证测试: {verifyTest}");
        
        if (!verifyTest)
        {
            Console.WriteLine($"[ChangePassword] 错误: 生成的密码哈希无法通过验证！");
            return StatusCode(500, new { message = "密码加密失败，请重试" });
        }

        holder.PasswordHash = newPasswordHash;
        holder.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        Console.WriteLine($"[ChangePassword] 密码修改成功: ID={id}");

        return NoContent();
    }

    /// <summary>
    /// 删除持有者（需无份额、无分红明细）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHolder(int id)
    {
        var holder = await _context.Holders.FindAsync(id);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        var hasShares = await _context.HolderShares.AnyAsync(hs => hs.HolderId == id);
        if (hasShares)
        {
            return BadRequest(new { message = "该持有人存在份额记录，无法删除" });
        }

        var hasDividendDetails = await _context.DividendDetails.AnyAsync(dd => dd.HolderId == id);
        if (hasDividendDetails)
        {
            return BadRequest(new { message = "该持有人存在分红记录，无法删除" });
        }

        _context.Holders.Remove(holder);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 创建持有者请求
/// </summary>
public class CreateHolderRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountName { get; set; }
}

/// <summary>
/// 更新持有者请求
/// </summary>
public class UpdateHolderRequest
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountName { get; set; }
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

