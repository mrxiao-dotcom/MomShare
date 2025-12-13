using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 投顾管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class AdvisorsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdvisorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取投顾列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Advisor>>> GetAdvisors()
    {
        return await _context.Advisors
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取投顾详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Advisor>> GetAdvisor(int id)
    {
        var advisor = await _context.Advisors
            .Include(a => a.Products)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (advisor == null)
        {
            return NotFound(new { message = "投顾不存在" });
        }

        return advisor;
    }

    /// <summary>
    /// 创建投顾
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Advisor>> CreateAdvisor([FromBody] CreateAdvisorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "投顾名称不能为空" });
        }

        var advisor = new Advisor
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        _context.Advisors.Add(advisor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAdvisor), new { id = advisor.Id }, advisor);
    }

    /// <summary>
    /// 更新投顾信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAdvisor(int id, [FromBody] UpdateAdvisorRequest request)
    {
        var advisor = await _context.Advisors.FindAsync(id);
        if (advisor == null)
        {
            return NotFound(new { message = "投顾不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "投顾名称不能为空" });
        }

        advisor.Name = request.Name;
        advisor.ContactPerson = request.ContactPerson;
        advisor.Phone = request.Phone;
        advisor.Email = request.Email;
        advisor.Remarks = request.Remarks;
        advisor.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 删除投顾
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAdvisor(int id)
    {
        var advisor = await _context.Advisors
            .Include(a => a.Products)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (advisor == null)
        {
            return NotFound(new { message = "投顾不存在" });
        }

        if (advisor.Products.Any())
        {
            return BadRequest(new { message = "该投顾正在使用中，无法删除" });
        }

        _context.Advisors.Remove(advisor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 创建投顾请求
/// </summary>
public class CreateAdvisorRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 更新投顾请求
/// </summary>
public class UpdateAdvisorRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
}

