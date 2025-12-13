using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 分配方案管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class DistributionPlansController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DistributionPlansController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取产品的分配方案
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<DistributionPlan>> GetProductDistributionPlan(int productId)
    {
        var plan = await _context.DistributionPlans
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (plan == null)
        {
            return NotFound(new { message = "分配方案不存在" });
        }

        return plan;
    }

    /// <summary>
    /// 更新分配方案
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDistributionPlan(int id, [FromBody] UpdateDistributionPlanRequest request)
    {
        var plan = await _context.DistributionPlans.FindAsync(id);
        if (plan == null)
        {
            return NotFound(new { message = "分配方案不存在" });
        }

        // 验证比例总和是否为100%
        var totalRatio = request.PriorityRatio + request.SubordinateRatio + 
                        request.ManagerRatio + request.AdvisorRatio;
        if (Math.Abs(totalRatio - 100m) > 0.01m)
        {
            return BadRequest(new { message = "分配比例总和必须等于100%" });
        }

        plan.PriorityRatio = request.PriorityRatio;
        plan.SubordinateRatio = request.SubordinateRatio;
        plan.ManagerRatio = request.ManagerRatio;
        plan.AdvisorRatio = request.AdvisorRatio;
        plan.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 更新分配方案请求
/// </summary>
public class UpdateDistributionPlanRequest
{
    public decimal PriorityRatio { get; set; }
    public decimal SubordinateRatio { get; set; }
    public decimal ManagerRatio { get; set; }
    public decimal AdvisorRatio { get; set; }
}

