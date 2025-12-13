using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 产品管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取产品列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products
            .Include(p => p.DistributionPlan)
            .Include(p => p.Advisor)
            .Include(p => p.Manager)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取产品详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.DistributionPlan)
            .Include(p => p.Advisor)
            .Include(p => p.Manager)
            .Include(p => p.NetValues.OrderByDescending(nv => nv.NetValueDate))
            .Include(p => p.HolderShares)
                .ThenInclude(hs => hs.Holder)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        return product;
    }

    /// <summary>
    /// 创建产品
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "产品名称不能为空" });
        }

        // 验证投顾和管理方是否存在
        if (request.AdvisorId.HasValue)
        {
            var advisor = await _context.Advisors.FindAsync(request.AdvisorId.Value);
            if (advisor == null)
            {
                return BadRequest(new { message = "投顾不存在" });
            }
        }

        if (request.ManagerId.HasValue)
        {
            var manager = await _context.Managers.FindAsync(request.ManagerId.Value);
            if (manager == null)
            {
                return BadRequest(new { message = "管理方不存在" });
            }
        }

        // 分配比例，默认 30/40/10/20，要求总和为 100%
        var priorityRatio = request.PriorityRatio ?? 30m;
        var subordinateRatio = request.SubordinateRatio ?? 40m;
        var managerRatio = request.ManagerRatio ?? 10m;
        var advisorRatio = request.AdvisorRatio ?? 20m;
        var ratioSum = priorityRatio + subordinateRatio + managerRatio + advisorRatio;
        if (Math.Abs(ratioSum - 100m) > 0.01m)
        {
            return BadRequest(new { message = "分配比例总和必须等于100%" });
        }

        var initialAmount = request.InitialAmount ?? 0;

        // 先创建产品
        var product = new Product
        {
            Name = request.Name,
            Code = request.Code,
            CurrentNetValue = 1.0m,
            TotalShares = 0,
            TotalAmount = initialAmount,
            InitialAmount = initialAmount,
            AdvisorId = request.AdvisorId,
            ManagerId = request.ManagerId,
            CreatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(); // 获取产品ID

        // 创建分配方案，绑定产品
        var distributionPlan = new DistributionPlan
        {
            ProductId = product.Id,
            PriorityRatio = priorityRatio,
            SubordinateRatio = subordinateRatio,
            ManagerRatio = managerRatio,
            AdvisorRatio = advisorRatio,
            CreatedAt = DateTime.Now
        };

        _context.DistributionPlans.Add(distributionPlan);
        await _context.SaveChangesAsync();

        // 回写产品的分配方案ID
        product.DistributionPlanId = distributionPlan.Id;
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// 更新产品信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.DistributionPlan)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "产品名称不能为空" });
        }

        // 验证投顾和管理方是否存在
        if (request.AdvisorId.HasValue)
        {
            var advisor = await _context.Advisors.FindAsync(request.AdvisorId.Value);
            if (advisor == null)
            {
                return BadRequest(new { message = "投顾不存在" });
            }
        }

        if (request.ManagerId.HasValue)
        {
            var manager = await _context.Managers.FindAsync(request.ManagerId.Value);
            if (manager == null)
            {
                return BadRequest(new { message = "管理方不存在" });
            }
        }

        if (request.TotalAmount.HasValue && request.TotalAmount.Value < 0)
        {
            return BadRequest(new { message = "当前总金额不能为负数" });
        }

        // 计算更新后的分配比例（使用现有值作为默认）
        var plan = product.DistributionPlan;
        var priorityRatio = request.PriorityRatio ?? plan?.PriorityRatio ?? 30m;
        var subordinateRatio = request.SubordinateRatio ?? plan?.SubordinateRatio ?? 40m;
        var managerRatio = request.ManagerRatio ?? plan?.ManagerRatio ?? 10m;
        var advisorRatio = request.AdvisorRatio ?? plan?.AdvisorRatio ?? 20m;
        var ratioSum = priorityRatio + subordinateRatio + managerRatio + advisorRatio;
        if (Math.Abs(ratioSum - 100m) > 0.01m)
        {
            return BadRequest(new { message = "分配比例总和必须等于100%" });
        }

        product.Name = request.Name;
        product.Code = request.Code;
        product.AdvisorId = request.AdvisorId;
        product.ManagerId = request.ManagerId;
        if (request.TotalAmount.HasValue)
        {
            product.TotalAmount = request.TotalAmount.Value;
        }
        product.UpdatedAt = DateTime.Now;

        // 更新或创建分配方案
        if (plan == null)
        {
            plan = new DistributionPlan
            {
                ProductId = product.Id,
                PriorityRatio = priorityRatio,
                SubordinateRatio = subordinateRatio,
                ManagerRatio = managerRatio,
                AdvisorRatio = advisorRatio,
                CreatedAt = DateTime.Now
            };
            _context.DistributionPlans.Add(plan);
        }
        else
        {
            plan.PriorityRatio = priorityRatio;
            plan.SubordinateRatio = subordinateRatio;
            plan.ManagerRatio = managerRatio;
            plan.AdvisorRatio = advisorRatio;
            plan.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 创建产品请求
/// </summary>
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal? InitialAmount { get; set; }
    public int? AdvisorId { get; set; }
    public int? ManagerId { get; set; }
    public decimal? PriorityRatio { get; set; }
    public decimal? SubordinateRatio { get; set; }
    public decimal? ManagerRatio { get; set; }
    public decimal? AdvisorRatio { get; set; }
}

/// <summary>
/// 更新产品请求
/// </summary>
public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int? AdvisorId { get; set; }
    public int? ManagerId { get; set; }
    public decimal? TotalAmount { get; set; } // 当前总金额可编辑
    public decimal? PriorityRatio { get; set; }
    public decimal? SubordinateRatio { get; set; }
    public decimal? ManagerRatio { get; set; }
    public decimal? AdvisorRatio { get; set; }
}

