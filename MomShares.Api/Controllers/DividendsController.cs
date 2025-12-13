using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 分红管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class DividendsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DividendsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建分红
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Dividend>> CreateDividend([FromBody] CreateDividendRequest request)
    {
        var product = await _context.Products
            .Include(p => p.DistributionPlan)
            .Include(p => p.HolderShares)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        if (product.DistributionPlan == null)
        {
            return BadRequest(new { message = "产品未配置分配方案" });
        }

        if (request.TotalAmount <= 0)
        {
            return BadRequest(new { message = "分红总额必须大于0" });
        }

        // 可分配金额 = 当前总权益 - 初始权益
        var distributable = product.TotalAmount - product.InitialAmount;
        if (distributable <= 0)
        {
            return BadRequest(new { message = "当前无可分配金额（总权益未超过初始权益）" });
        }
        if (request.TotalAmount > distributable)
        {
            return BadRequest(new { message = $"分红金额超出可分配额度（最大 {distributable}）" });
        }

        // 获取所有持有者份额
        var allHolderShares = await _context.HolderShares
            .Where(hs => hs.ProductId == request.ProductId)
            .ToListAsync();

        if (allHolderShares.Count == 0)
        {
            return BadRequest(new { message = "该产品没有持有者" });
        }

        // 分离优先方和劣后方份额
        var priorityShares = allHolderShares.Where(hs => hs.ShareType == "Priority").ToList();
        var subordinateShares = allHolderShares.Where(hs => hs.ShareType == "Subordinate").ToList();

        // 计算优先方和劣后方的总份额
        var priorityTotalShares = priorityShares.Sum(hs => hs.ShareAmount);
        var subordinateTotalShares = subordinateShares.Sum(hs => hs.ShareAmount);

        if (priorityTotalShares == 0 && subordinateTotalShares == 0)
        {
            return BadRequest(new { message = "产品总份额为0" });
        }

        var plan = product.DistributionPlan;

        // 计算各方分红金额（按分配方案比例）
        var priorityAmount = request.TotalAmount * (plan.PriorityRatio / 100m);
        var subordinateAmount = request.TotalAmount * (plan.SubordinateRatio / 100m);
        var managerAmount = request.TotalAmount * (plan.ManagerRatio / 100m);
        var advisorAmount = request.TotalAmount * (plan.AdvisorRatio / 100m);

        // 校验劣后方持有人存在性（优先方无需校验）
        if (subordinateAmount > 0 && subordinateTotalShares <= 0)
        {
            return BadRequest(new { message = "劣后方分配金额大于0，但未找到劣后方持有人" });
        }

        // 创建分红记录
        var dividend = new Dividend
        {
            ProductId = request.ProductId,
            DividendDate = request.DividendDate,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.Now
        };

        _context.Dividends.Add(dividend);
        await _context.SaveChangesAsync(); // 先保存以获取ID

        // 计算并创建分红明细
        var dividendDetails = new List<DividendDetail>();

        // 劣后方投资人分红明细（按份额占比分配）
        if (subordinateTotalShares > 0 && subordinateAmount > 0)
        {
            foreach (var holderShare in subordinateShares)
            {
                var ratio = holderShare.ShareAmount / subordinateTotalShares;
                var amount = subordinateAmount * ratio;

                dividendDetails.Add(new DividendDetail
                {
                    DividendId = dividend.Id,
                    HolderId = holderShare.HolderId,
                    Amount = amount,
                    ShareRatio = ratio,
                    CreatedAt = DateTime.Now
                });
            }
        }

        _context.DividendDetails.AddRange(dividendDetails);

        // 创建管理方和投顾方的分红分配记录
        var distributions = new List<DividendDistribution>();

        if (product.ManagerId.HasValue && managerAmount > 0)
        {
            distributions.Add(new DividendDistribution
            {
                DividendId = dividend.Id,
                DistributionType = "Manager",
                ManagerId = product.ManagerId.Value,
                Amount = managerAmount,
                Ratio = plan.ManagerRatio,
                CreatedAt = DateTime.Now
            });
        }

        if (product.AdvisorId.HasValue && advisorAmount > 0)
        {
            distributions.Add(new DividendDistribution
            {
                DividendId = dividend.Id,
                DistributionType = "Advisor",
                AdvisorId = product.AdvisorId.Value,
                Amount = advisorAmount,
                Ratio = plan.AdvisorRatio,
                CreatedAt = DateTime.Now
            });
        }

        _context.DividendDistributions.AddRange(distributions);

        // 分红后，产品总金额减少（分红总额），初始权益保持不变
        product.TotalAmount -= request.TotalAmount;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // 重新加载包含明细的分红记录
        await _context.Entry(dividend)
            .Collection(d => d.DividendDetails)
            .LoadAsync();
        await _context.Entry(dividend)
            .Collection(d => d.DividendDistributions)
            .LoadAsync();

        return CreatedAtAction(nameof(GetDividend), new { id = dividend.Id }, dividend);
    }

    /// <summary>
    /// 获取分红记录
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Dividend>> GetDividend(int id)
    {
        var dividend = await _context.Dividends
            .Include(d => d.DividendDetails)
                .ThenInclude(dd => dd.Holder)
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dividend == null)
        {
            return NotFound(new { message = "分红记录不存在" });
        }

        return dividend;
    }

    /// <summary>
    /// 获取产品分红记录列表
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetProductDividends(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        var plan = await _context.DistributionPlans
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (plan == null)
        {
            return BadRequest(new { message = "产品未配置分配方案" });
        }

        var list = await _context.Dividends
            .Where(d => d.ProductId == productId)
            .OrderByDescending(d => d.DividendDate)
            .Select(d => new
            {
                d.Id,
                d.ProductId,
                d.TotalAmount,
                d.DividendDate,
                d.CreatedAt,
                PriorityAmount = d.TotalAmount * (plan.PriorityRatio / 100m),
                SubordinateAmount = d.TotalAmount * (plan.SubordinateRatio / 100m),
                ManagerAmount = d.TotalAmount * (plan.ManagerRatio / 100m),
                AdvisorAmount = d.TotalAmount * (plan.AdvisorRatio / 100m)
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// 获取产品当前持有人份额及分红预览（优先/劣后按持股比例）
    /// </summary>
    [HttpGet("product/{productId}/preview")]
    public async Task<ActionResult<object>> GetDividendPreview(int productId, [FromQuery] decimal amount)
    {
        var product = await _context.Products
            .Include(p => p.DistributionPlan)
            .Include(p => p.HolderShares)
                .ThenInclude(hs => hs.Holder)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }
        if (product.DistributionPlan == null)
        {
            return BadRequest(new { message = "产品未配置分配方案" });
        }
        if (amount <= 0)
        {
            return BadRequest(new { message = "分红金额必须大于0" });
        }

        var plan = product.DistributionPlan;
        var priorityAmount = amount * (plan.PriorityRatio / 100m);
        var subordinateAmount = amount * (plan.SubordinateRatio / 100m);
        var managerAmount = amount * (plan.ManagerRatio / 100m);
        var advisorAmount = amount * (plan.AdvisorRatio / 100m);

        var subordinateShares = product.HolderShares.Where(hs => hs.ShareType == "Subordinate").ToList();
        var subordinateTotal = subordinateShares.Sum(hs => hs.ShareAmount);

        var subordinateDetails = subordinateTotal > 0
            ? subordinateShares.Select(hs => new
            {
                HolderId = hs.HolderId,
                HolderName = hs.Holder.Name,
                ShareAmount = hs.ShareAmount,
                Ratio = hs.ShareAmount / subordinateTotal,
                Amount = subordinateAmount * (hs.ShareAmount / subordinateTotal)
            })
            : Enumerable.Empty<object>();

        return Ok(new
        {
            PriorityAmount = priorityAmount,
            SubordinateAmount = subordinateAmount,
            ManagerAmount = managerAmount,
            AdvisorAmount = advisorAmount,
            PriorityDetails = Enumerable.Empty<object>(), // 优先方不展示明细
            SubordinateDetails = subordinateDetails
        });
    }

    /// <summary>
    /// 获取持有者分红记录列表
    /// </summary>
    [HttpGet("holder/{holderId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetHolderDividends(int holderId)
    {
        var holder = await _context.Holders.FindAsync(holderId);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        var dividendDetails = await _context.DividendDetails
            .Include(dd => dd.Dividend)
                .ThenInclude(d => d!.Product)
            .Where(dd => dd.HolderId == holderId)
            .OrderByDescending(dd => dd.Dividend!.DividendDate)
            .Select(dd => new
            {
                id = dd.Id,
                dividendId = dd.DividendId,
                productId = dd.Dividend!.ProductId,
                productName = dd.Dividend.Product!.Name,
                dividendDate = dd.Dividend.DividendDate,
                amount = dd.Amount,
                shareRatio = dd.ShareRatio
            })
            .ToListAsync();

        return Ok(dividendDetails);
    }

    /// <summary>
    /// 获取全部分红记录（含产品名称）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAllDividends()
    {
        var list = await _context.Dividends
            .Include(d => d.Product)
            .OrderByDescending(d => d.DividendDate)
            .Select(d => new
            {
                d.Id,
                d.ProductId,
                ProductName = d.Product.Name,
                d.DividendDate,
                d.TotalAmount
            })
            .ToListAsync();

        return Ok(list);
    }
}

/// <summary>
/// 创建分红请求
/// </summary>
public class CreateDividendRequest
{
    public int ProductId { get; set; }
    public DateTime DividendDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
}

