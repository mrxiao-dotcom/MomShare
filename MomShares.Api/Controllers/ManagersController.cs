using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 管理方管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class ManagersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ManagersController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取管理方列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Manager>>> GetManagers()
    {
        return await _context.Managers
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取管理方详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Manager>> GetManager(int id)
    {
        var manager = await _context.Managers
            .Include(m => m.Products)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manager == null)
        {
            return NotFound(new { message = "管理方不存在" });
        }

        return manager;
    }

    /// <summary>
    /// 创建管理方
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Manager>> CreateManager([FromBody] CreateManagerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "管理方名称不能为空" });
        }

        var manager = new Manager
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        _context.Managers.Add(manager);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetManager), new { id = manager.Id }, manager);
    }

    /// <summary>
    /// 更新管理方信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateManager(int id, [FromBody] UpdateManagerRequest request)
    {
        var manager = await _context.Managers.FindAsync(id);
        if (manager == null)
        {
            return NotFound(new { message = "管理方不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "管理方名称不能为空" });
        }

        manager.Name = request.Name;
        manager.ContactPerson = request.ContactPerson;
        manager.Phone = request.Phone;
        manager.Email = request.Email;
        manager.Remarks = request.Remarks;
        manager.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 删除管理方
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteManager(int id)
    {
        var manager = await _context.Managers
            .Include(m => m.Products)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manager == null)
        {
            return NotFound(new { message = "管理方不存在" });
        }

        if (manager.Products.Any())
        {
            return BadRequest(new { message = "该管理方正在使用中，无法删除" });
        }

        _context.Managers.Remove(manager);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 创建管理方请求
/// </summary>
public class CreateManagerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 更新管理方请求
/// </summary>
public class UpdateManagerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
}

