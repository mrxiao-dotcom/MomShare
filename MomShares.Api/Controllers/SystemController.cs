using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Core.Interfaces;
using MomShares.Infrastructure.Data;
using System.IO.Compression;

namespace MomShares.Api.Controllers;

/// <summary>
/// 系统管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class SystemController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;

    public SystemController(
        ApplicationDbContext context,
        IConfiguration configuration,
        IPasswordService passwordService)
    {
        _context = context;
        _configuration = configuration;
        _passwordService = passwordService;
    }

    /// <summary>
    /// 手动备份数据库
    /// </summary>
    [HttpPost("backup")]
    public ActionResult BackupDatabase()
    {
        try
        {
            var backupPath = _configuration["BackupSettings:BackupPath"] ?? "D:\\Backups\\MomShares";
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            var dbPath = _configuration.GetConnectionString("DefaultConnection")?.Replace("Data Source=", "") ?? "MomShares.db";
            if (!System.IO.File.Exists(dbPath))
            {
                return BadRequest(new { message = "数据库文件不存在" });
            }

            var backupFileName = $"MomShares_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupFilePath = Path.Combine(backupPath, backupFileName);

            System.IO.File.Copy(dbPath, backupFilePath, true);

            return Ok(new { message = "备份成功", filePath = backupFilePath, fileName = backupFileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "备份失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取备份列表
    /// </summary>
    [HttpGet("backups")]
    public ActionResult<IEnumerable<object>> GetBackups()
    {
        try
        {
            var backupPath = _configuration["BackupSettings:BackupPath"] ?? "D:\\Backups\\MomShares";
            if (!Directory.Exists(backupPath))
            {
                return Ok(new List<object>());
            }

            var backupFiles = Directory.GetFiles(backupPath, "MomShares_backup_*.db");
            var backups = backupFiles
                .Select(backupFile => new
                {
                    fileName = Path.GetFileName(backupFile),
                    filePath = backupFile,
                    fileSize = new FileInfo(backupFile).Length,
                    createdTime = System.IO.File.GetCreationTime(backupFile)
                })
                .OrderByDescending(b => b.createdTime)
                .ToList();

            return Ok(backups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "获取备份列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 恢复数据库
    /// </summary>
    [HttpPost("restore")]
    public ActionResult RestoreDatabase([FromBody] RestoreRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BackupFilePath))
            {
                return BadRequest(new { message = "备份文件路径不能为空" });
            }

            if (!System.IO.File.Exists(request.BackupFilePath))
            {
                return NotFound(new { message = "备份文件不存在" });
            }

            var dbPath = _configuration.GetConnectionString("DefaultConnection")?.Replace("Data Source=", "") ?? "MomShares.db";

            // 恢复前先备份当前数据库
            if (System.IO.File.Exists(dbPath))
            {
                var backupPath = _configuration["BackupSettings:BackupPath"] ?? "D:\\Backups\\MomShares";
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                var preRestoreBackup = Path.Combine(backupPath, $"MomShares_pre_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                System.IO.File.Copy(dbPath, preRestoreBackup, true);
            }

            // 恢复数据库
            System.IO.File.Copy(request.BackupFilePath, dbPath, true);

            return Ok(new { message = "恢复成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "恢复失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取管理员列表
    /// </summary>
    [HttpGet("admins")]
    public async Task<ActionResult<IEnumerable<object>>> GetAdmins()
    {
        var admins = await _context.Admins
            .Select(a => new
            {
                id = a.Id,
                username = a.Username,
                createdAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(admins);
    }

    /// <summary>
    /// 创建管理员
    /// </summary>
    [HttpPost("admins")]
    public async Task<ActionResult<Admin>> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "用户名不能为空" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "密码不能为空" });
        }

        if (await _context.Admins.AnyAsync(a => a.Username == request.Username))
        {
            return BadRequest(new { message = "用户名已存在" });
        }

        var admin = new Admin
        {
            Username = request.Username,
            PasswordHash = _passwordService.HashPassword(request.Password),
            CreatedAt = DateTime.Now
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        admin.PasswordHash = string.Empty; // 不返回密码哈希

        return CreatedAtAction(nameof(GetAdmins), new { }, admin);
    }

    /// <summary>
    /// 修改管理员密码
    /// </summary>
    [HttpPut("admins/{id}/password")]
    public async Task<IActionResult> ChangeAdminPassword(int id, [FromBody] ChangeAdminPasswordRequest request)
    {
        var admin = await _context.Admins.FindAsync(id);
        if (admin == null)
        {
            return NotFound(new { message = "管理员不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "新密码不能为空" });
        }

        admin.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        admin.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取操作日志
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<OperationLog>>> GetLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? operationType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.OperationLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(log => log.OperationTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(log => log.OperationTime <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(operationType))
        {
            query = query.Where(log => log.OperationType == operationType);
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(log => log.OperationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = logs,
            totalCount = totalCount,
            page = page,
            pageSize = pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }
}

/// <summary>
/// 恢复请求
/// </summary>
public class RestoreRequest
{
    public string BackupFilePath { get; set; } = string.Empty;
}

/// <summary>
/// 创建管理员请求
/// </summary>
public class CreateAdminRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 修改管理员密码请求
/// </summary>
public class ChangeAdminPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

