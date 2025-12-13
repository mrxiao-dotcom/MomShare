using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;
using System.Text.Json;

namespace MomShares.Api.Controllers;

/// <summary>
/// 增资管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class CapitalIncreasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CapitalIncreasesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建增减资记录（包含份额重分配）
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CapitalIncrease>> CreateCapitalIncrease([FromBody] CreateCapitalIncreaseRequest request)
    {
        var product = await _context.Products
            .Include(p => p.HolderShares)
                .ThenInclude(hs => hs.Holder)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 净值必须为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有净值为1时才能增减资" });
        }

        // 当前权益需等于初始权益（容差 0.0001）
        if (Math.Abs(product.TotalAmount - product.InitialAmount) > 0.0001m)
        {
            return BadRequest(new { message = "当前权益需等于初始权益时才可增减资" });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "金额必须大于0" });
        }

        if (request.Allocations == null || request.Allocations.Count == 0)
        {
            return BadRequest(new { message = "持有人分配不能为空" });
        }

        var isDecrease = string.Equals(request.Type, "Decrease", StringComparison.OrdinalIgnoreCase);
        var delta = isDecrease ? -request.Amount : request.Amount;
        var amountBefore = product.TotalAmount;
        var amountAfter = amountBefore + delta;

        if (amountAfter < 0)
        {
            return BadRequest(new { message = "减资后金额不能小于0" });
        }

        // 校验持有人分配
        var allocationIds = request.Allocations.Select(a => a.HolderId).ToList();
        if (allocationIds.Distinct().Count() != allocationIds.Count)
        {
            return BadRequest(new { message = "分配列表中存在重复持有人" });
        }

        var holderIds = request.Allocations.Select(a => a.HolderId).ToList();
        var holderNameMap = await _context.Holders
            .Where(h => holderIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Name);

        // 确保所有持有人存在
        var missingHolders = holderIds.Where(id => !holderNameMap.ContainsKey(id)).Distinct().ToList();
        if (missingHolders.Any())
        {
            return BadRequest(new { message = $"以下持有人不存在: {string.Join(",", missingHolders)}" });
        }

        var existingShares = product.HolderShares.ToDictionary(hs => hs.HolderId, hs => hs);

        // 减资仅允许现有持有人
        if (isDecrease)
        {
            var newIds = allocationIds.Except(existingShares.Keys).ToList();
            if (newIds.Any())
            {
                return BadRequest(new { message = "减资只允许现有持有人参与" });
            }

            // 要求覆盖全部现有持有人，避免遗漏
            var missing = existingShares.Keys.Except(allocationIds).ToList();
            if (missing.Any())
            {
                return BadRequest(new { message = "请为所有现有持有人提供减资后的份额（可为0）" });
            }
        }

        // 计算分配总额需等于新的初始/当前权益（1元=1份）
        var totalAllocation = request.Allocations.Sum(a => a.ShareAmount);
        if (Math.Abs(totalAllocation - amountAfter) > 0.0001m)
        {
            return BadRequest(new { message = $"分配总份额({totalAllocation})必须等于新的权益金额({amountAfter})，1元1份" });
        }

        // 校验减资份额不得超过原份额
        if (isDecrease)
        {
            foreach (var alloc in request.Allocations)
            {
                var oldShare = existingShares[alloc.HolderId];
                if (alloc.ShareAmount > oldShare.ShareAmount)
                {
                    return BadRequest(new { message = $"持有人 {oldShare.Holder?.Name ?? oldShare.HolderId.ToString()} 的份额不能增加（减资只允许减少或清零）" });
                }
            }
        }

        var now = DateTime.Now;
        var transactions = new List<ShareTransaction>();

        // 更新/新增持有人份额
        foreach (var alloc in request.Allocations)
        {
            var hasOld = existingShares.TryGetValue(alloc.HolderId, out var oldShare);
            var newAmount = alloc.ShareAmount;

            if (hasOld)
            {
                var deltaShare = newAmount - oldShare!.ShareAmount;
                oldShare.ShareAmount = newAmount;
                oldShare.InvestmentAmount = newAmount; // 净值=1
                oldShare.UpdatedAt = now;

                if (deltaShare != 0)
                {
                    transactions.Add(new ShareTransaction
                    {
                        HolderId = alloc.HolderId,
                        ProductId = request.ProductId,
                        TransactionType = deltaShare > 0 ? Core.Enums.ShareTransactionType.Add : Core.Enums.ShareTransactionType.Reduce,
                        TransactionDate = request.IncreaseDate,
                        ShareChange = deltaShare,
                        TransactionPrice = 1m,
                        NetValueAtTime = product.CurrentNetValue,
                        Remarks = request.Remarks,
                        CreatedAt = now
                    });
                }
            }
            else
            {
                // 仅增资允许新增持有人
                if (isDecrease)
                {
                    return BadRequest(new { message = "减资不允许新增持有人" });
                }

                var shareType = string.IsNullOrWhiteSpace(alloc.ShareType) ? "Subordinate" : alloc.ShareType!;
                var newShare = new HolderShare
                {
                    HolderId = alloc.HolderId,
                    ProductId = request.ProductId,
                    ShareAmount = newAmount,
                    InvestmentAmount = newAmount,
                    ShareType = shareType,
                    CreatedAt = now
                };
                _context.HolderShares.Add(newShare);

                transactions.Add(new ShareTransaction
                {
                    HolderId = alloc.HolderId,
                    ProductId = request.ProductId,
                    TransactionType = Core.Enums.ShareTransactionType.Add,
                    TransactionDate = request.IncreaseDate,
                    ShareChange = newAmount,
                    TransactionPrice = 1m,
                    NetValueAtTime = product.CurrentNetValue,
                    Remarks = request.Remarks,
                    CreatedAt = now
                });
            }
        }

        // 删除未出现且份额应为0的旧持有人（仅减资且分配为0）
        if (isDecrease)
        {
            var zeroAllocIds = request.Allocations.Where(a => a.ShareAmount == 0).Select(a => a.HolderId).ToHashSet();
            var toRemove = product.HolderShares.Where(hs => zeroAllocIds.Contains(hs.HolderId)).ToList();
            if (toRemove.Any())
            {
                _context.HolderShares.RemoveRange(toRemove);
            }
        }

        if (transactions.Any())
        {
            _context.ShareTransactions.AddRange(transactions);
        }

        // 更新产品金额/份额/初始权益
        product.TotalShares = totalAllocation;
        product.TotalAmount = amountAfter;
        product.InitialAmount = amountAfter;
        product.UpdatedAt = now;

        // 记录增减资
        var detailPayload = request.Allocations.Select(a => new
        {
            a.HolderId,
            HolderName = holderNameMap.GetValueOrDefault(a.HolderId),
            a.ShareAmount,
            ShareType = string.IsNullOrWhiteSpace(a.ShareType)
                ? (existingShares.TryGetValue(a.HolderId, out var hs) ? hs.ShareType : "Subordinate")
                : a.ShareType
        });

        var capitalIncrease = new CapitalIncrease
        {
            ProductId = request.ProductId,
            IncreaseDate = request.IncreaseDate,
            AmountBefore = amountBefore,
            IncreaseAmount = request.Amount,
            AmountAfter = amountAfter,
            Type = isDecrease ? "Decrease" : "Increase",
            Details = JsonSerializer.Serialize(detailPayload),
            CreatedAt = now
        };

        _context.CapitalIncreases.Add(capitalIncrease);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCapitalIncrease), new { id = capitalIncrease.Id }, capitalIncrease);
    }

    /// <summary>
    /// 获取全部增减资记录
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetCapitalIncreases()
    {
        var list = await _context.CapitalIncreases
            .Include(ci => ci.Product)
            .OrderByDescending(ci => ci.IncreaseDate)
            .Select(ci => new
            {
                ci.Id,
                ci.ProductId,
                ProductName = ci.Product != null ? ci.Product.Name : null,
                ci.Type,
                ci.AmountBefore,
                ci.IncreaseAmount,
                ci.AmountAfter,
                ci.IncreaseDate,
                ci.Details,
                ci.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// 获取增资记录
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CapitalIncrease>> GetCapitalIncrease(int id)
    {
        var capitalIncrease = await _context.CapitalIncreases
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == id);

        if (capitalIncrease == null)
        {
            return NotFound(new { message = "增资记录不存在" });
        }

        return capitalIncrease;
    }

    /// <summary>
    /// 删除增减资记录
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCapitalIncrease(int id)
    {
        var record = await _context.CapitalIncreases.FindAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "记录不存在" });
        }

        _context.CapitalIncreases.Remove(record);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 获取产品增资历史记录
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<CapitalIncrease>>> GetProductCapitalIncreases(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        var list = await _context.CapitalIncreases
            .Where(ci => ci.ProductId == productId)
            .OrderByDescending(ci => ci.IncreaseDate)
            .Select(ci => new
            {
                ci.Id,
                ci.ProductId,
                ci.Type,
                ci.AmountBefore,
                ci.IncreaseAmount,
                ci.AmountAfter,
                ci.IncreaseDate,
                ci.Details,
                ci.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }
}

/// <summary>
/// 创建增资请求
/// </summary>
public class CreateCapitalIncreaseRequest
{
    public int ProductId { get; set; }
    public DateTime IncreaseDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    /// <summary>
    /// 类型：Increase / Decrease
    /// </summary>
    public string Type { get; set; } = "Increase";
    /// <summary>
    /// 份额分配（1元1份，合计需等于新的初始权益）
    /// </summary>
    public List<CapitalAllocationRequest> Allocations { get; set; } = new();
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}

public class CapitalAllocationRequest
{
    public int HolderId { get; set; }
    public decimal ShareAmount { get; set; }
    public string? ShareType { get; set; }
}

