using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Core.Enums;
using MomShares.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace MomShares.Api.Controllers;

/// <summary>
/// 份额管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class SharesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SharesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有份额列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAllShares()
    {
        var shares = await _context.HolderShares
            .Include(hs => hs.Product)
            .Include(hs => hs.Holder)
            .Select(hs => new
            {
                Id = hs.Id,
                ProductId = hs.ProductId,
                ProductName = hs.Product!.Name,
                HolderId = hs.HolderId,
                HolderName = hs.Holder!.Name,
                ShareAmount = hs.ShareAmount,
                InvestmentAmount = hs.InvestmentAmount,
                CreatedAt = hs.CreatedAt,
                UpdatedAt = hs.UpdatedAt
            })
            .OrderByDescending(hs => hs.CreatedAt)
            .ToListAsync();

        return Ok(shares);
    }

    /// <summary>
    /// 获取持有者份额列表
    /// </summary>
    [HttpGet("holder/{holderId}")]
    public async Task<ActionResult<IEnumerable<HolderShare>>> GetHolderShares(int holderId)
    {
        var holder = await _context.Holders.FindAsync(holderId);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        return await _context.HolderShares
            .Include(hs => hs.Product)
            .Where(hs => hs.HolderId == holderId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取产品份额列表
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<HolderShare>>> GetProductShares(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        return await _context.HolderShares
            .Include(hs => hs.Holder)
            .Where(hs => hs.ProductId == productId)
            .ToListAsync();
    }

    /// <summary>
    /// 确定份额（初始出资）
    /// </summary>
    [HttpPost("initial")]
    public async Task<ActionResult<HolderShare>> CreateInitialShare([FromBody] CreateInitialShareRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        var holder = await _context.Holders.FindAsync(request.HolderId);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        // 检查是否已有关联（同一类型）
        var shareType = request.ShareType ?? "Subordinate";
        var existingShare = await _context.HolderShares
            .FirstOrDefaultAsync(hs => hs.HolderId == request.HolderId && 
                                      hs.ProductId == request.ProductId && 
                                      hs.ShareType == shareType);

        if (existingShare != null)
        {
            return BadRequest(new { message = $"该持有者已关联此产品的{shareType}份额，请使用新增份额或转让功能" });
        }

        // 验证净值为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额操作" });
        }

        var holderShare = new HolderShare
        {
            HolderId = request.HolderId,
            ProductId = request.ProductId,
            ShareAmount = request.ShareAmount,
            InvestmentAmount = request.InvestmentAmount,
            ShareType = request.ShareType ?? "Subordinate", // 默认为劣后方
            CreatedAt = DateTime.Now
        };

        _context.HolderShares.Add(holderShare);

        // 更新产品总份额和总金额
        product.TotalShares += request.ShareAmount;
        product.TotalAmount += request.InvestmentAmount;
        product.UpdatedAt = DateTime.Now;

        // 记录操作
        var transaction = new ShareTransaction
        {
            HolderId = request.HolderId,
            ProductId = request.ProductId,
            TransactionType = ShareTransactionType.InitialInvestment,
            TransactionDate = request.TransactionDate,
            ShareChange = request.ShareAmount,
            TransactionPrice = request.InvestmentAmount / request.ShareAmount,
            NetValueAtTime = product.CurrentNetValue,
            CreatedAt = DateTime.Now
        };

        _context.ShareTransactions.Add(transaction);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHolderShares), new { holderId = request.HolderId }, holderShare);
    }

    /// <summary>
    /// 份额转让
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult> TransferShare([FromBody] TransferShareRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 验证净值为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额操作" });
        }

        var fromHolder = await _context.Holders.FindAsync(request.FromHolderId);
        var toHolder = await _context.Holders.FindAsync(request.ToHolderId);

        if (fromHolder == null || toHolder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        var fromShare = await _context.HolderShares
            .FirstOrDefaultAsync(hs => hs.HolderId == request.FromHolderId && hs.ProductId == request.ProductId);

        if (fromShare == null || fromShare.ShareAmount < request.ShareAmount)
        {
            return BadRequest(new { message = "转出方份额不足" });
        }

        // 更新转出方份额
        fromShare.ShareAmount -= request.ShareAmount;
        fromShare.InvestmentAmount -= request.TransactionPrice * request.ShareAmount;
        fromShare.UpdatedAt = DateTime.Now;

        // 更新或创建转入方份额
        var toShare = await _context.HolderShares
            .FirstOrDefaultAsync(hs => hs.HolderId == request.ToHolderId && hs.ProductId == request.ProductId);

        if (toShare == null)
        {
            toShare = new HolderShare
            {
                HolderId = request.ToHolderId,
                ProductId = request.ProductId,
                ShareAmount = request.ShareAmount,
                InvestmentAmount = request.TransactionPrice * request.ShareAmount,
                CreatedAt = DateTime.Now
            };
            _context.HolderShares.Add(toShare);
        }
        else
        {
            toShare.ShareAmount += request.ShareAmount;
            toShare.InvestmentAmount += request.TransactionPrice * request.ShareAmount;
            toShare.UpdatedAt = DateTime.Now;
        }

        // 记录操作
        var fromTransaction = new ShareTransaction
        {
            HolderId = request.FromHolderId,
            ProductId = request.ProductId,
            TransactionType = ShareTransactionType.Transfer,
            TransactionDate = request.TransactionDate,
            ShareChange = -request.ShareAmount,
            TransactionPrice = request.TransactionPrice,
            NetValueAtTime = product.CurrentNetValue,
            CounterpartyId = request.ToHolderId,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        var toTransaction = new ShareTransaction
        {
            HolderId = request.ToHolderId,
            ProductId = request.ProductId,
            TransactionType = ShareTransactionType.Transfer,
            TransactionDate = request.TransactionDate,
            ShareChange = request.ShareAmount,
            TransactionPrice = request.TransactionPrice,
            NetValueAtTime = product.CurrentNetValue,
            CounterpartyId = request.FromHolderId,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        _context.ShareTransactions.AddRange(fromTransaction, toTransaction);

        await _context.SaveChangesAsync();

        return Ok(new { message = "份额转让成功" });
    }

    /// <summary>
    /// 新增份额
    /// </summary>
    [HttpPost("add")]
    public async Task<ActionResult> AddShare([FromBody] AddShareRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 验证净值为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额操作" });
        }

        var holder = await _context.Holders.FindAsync(request.HolderId);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        var holderShare = await _context.HolderShares
            .FirstOrDefaultAsync(hs => hs.HolderId == request.HolderId && hs.ProductId == request.ProductId);

        if (holderShare == null)
        {
            return BadRequest(new { message = "持有者未关联此产品，请先使用确定份额功能" });
        }

        holderShare.ShareAmount += request.ShareAmount;
        holderShare.InvestmentAmount += request.InvestmentAmount;
        holderShare.UpdatedAt = DateTime.Now;

        product.TotalShares += request.ShareAmount;
        product.TotalAmount += request.InvestmentAmount;
        product.UpdatedAt = DateTime.Now;

        var transaction = new ShareTransaction
        {
            HolderId = request.HolderId,
            ProductId = request.ProductId,
            TransactionType = ShareTransactionType.Add,
            TransactionDate = request.TransactionDate,
            ShareChange = request.ShareAmount,
            TransactionPrice = request.InvestmentAmount / request.ShareAmount,
            NetValueAtTime = product.CurrentNetValue,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        _context.ShareTransactions.Add(transaction);

        await _context.SaveChangesAsync();

        return Ok(new { message = "新增份额成功" });
    }

    /// <summary>
    /// 减持份额
    /// </summary>
    [HttpPost("reduce")]
    public async Task<ActionResult> ReduceShare([FromBody] ReduceShareRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 验证净值为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额操作" });
        }

        var holder = await _context.Holders.FindAsync(request.HolderId);
        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        var holderShare = await _context.HolderShares
            .FirstOrDefaultAsync(hs => hs.HolderId == request.HolderId && hs.ProductId == request.ProductId);

        if (holderShare == null || holderShare.ShareAmount < request.ShareAmount)
        {
            return BadRequest(new { message = "份额不足" });
        }

        holderShare.ShareAmount -= request.ShareAmount;
        holderShare.InvestmentAmount -= request.RecoveryAmount;
        holderShare.UpdatedAt = DateTime.Now;

        product.TotalShares -= request.ShareAmount;
        product.TotalAmount -= request.RecoveryAmount;
        product.UpdatedAt = DateTime.Now;

        var transaction = new ShareTransaction
        {
            HolderId = request.HolderId,
            ProductId = request.ProductId,
            TransactionType = ShareTransactionType.Reduce,
            TransactionDate = request.TransactionDate,
            ShareChange = -request.ShareAmount,
            TransactionPrice = request.RecoveryAmount / request.ShareAmount,
            NetValueAtTime = product.CurrentNetValue,
            Remarks = request.Remarks,
            CreatedAt = DateTime.Now
        };

        _context.ShareTransactions.Add(transaction);

        await _context.SaveChangesAsync();

        return Ok(new { message = "减持份额成功" });
    }

    /// <summary>
    /// 为产品分配持有人份额（初始配置，1元1份，份额总额必须等于产品当前总权益）
    /// 仅允许在该产品还没有份额记录时进行
    /// </summary>
    [HttpPost("product/{productId}/allocate")]
    public async Task<ActionResult> AllocateProductShares(int productId, [FromBody] ProductAllocationRequest request)
    {
        if (request.Allocations == null || request.Allocations.Count == 0)
        {
            return BadRequest(new { message = "分配数据不能为空" });
        }

        var product = await _context.Products
            .Include(p => p.HolderShares)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 若已有份额，暂不允许覆盖，避免误操作
        if (product.HolderShares.Any())
        {
            return BadRequest(new { message = "该产品已存在份额记录，如需重新分配请先清空旧份额" });
        }

        // 当前净值必须为1，遵循原有份额操作规则
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额分配" });
        }

        // 检查持有人是否存在、重复
        var holderIds = request.Allocations.Select(a => a.HolderId).ToList();
        if (holderIds.Distinct().Count() != holderIds.Count)
        {
            return BadRequest(new { message = "同一产品的份额分配中，持有人不能重复" });
        }

        var existingHolders = await _context.Holders
            .Where(h => holderIds.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();
        var missing = holderIds.Except(existingHolders).ToList();
        if (missing.Any())
        {
            return BadRequest(new { message = $"以下持有人不存在: {string.Join(",", missing)}" });
        }

        // 计算总份额（1元1份），要求等于产品当前总权益
        var totalAllocation = request.Allocations.Sum(a => a.ShareAmount);
        if (totalAllocation != product.TotalAmount)
        {
            return BadRequest(new { message = $"分配总份额({totalAllocation})必须等于产品总权益({product.TotalAmount})，1元1份" });
        }

        // 构建份额记录
        var now = DateTime.Now;
        foreach (var allocation in request.Allocations)
        {
            var share = new HolderShare
            {
                HolderId = allocation.HolderId,
                ProductId = productId,
                ShareAmount = allocation.ShareAmount,
                InvestmentAmount = allocation.InvestmentAmount ?? allocation.ShareAmount, // 如果没有指定投资金额，默认等于份额
                ShareType = string.IsNullOrWhiteSpace(allocation.ShareType) ? "Subordinate" : allocation.ShareType,
                CreatedAt = now
            };
            _context.HolderShares.Add(share);
        }

        // 更新产品总份额与总金额保持一致
        product.TotalShares = totalAllocation;
        product.TotalAmount = totalAllocation;
        product.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return Ok(new { message = "份额分配成功" });
    }

    /// <summary>
    /// 更新产品份额分配（编辑现有份额和投资金额）
    /// </summary>
    [HttpPut("product/{productId}/allocate")]
    public async Task<ActionResult> UpdateProductShares(int productId, [FromBody] ProductAllocationRequest request)
    {
        if (request.Allocations == null || request.Allocations.Count == 0)
        {
            return BadRequest(new { message = "分配数据不能为空" });
        }

        var product = await _context.Products
            .Include(p => p.HolderShares)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 验证净值为1
        if (Math.Abs(product.CurrentNetValue - 1.0m) > 0.0001m)
        {
            return BadRequest(new { message = "只有产品净值为1时才能进行份额操作" });
        }

        // 检查持有人是否存在、重复
        var holderIds = request.Allocations.Select(a => a.HolderId).ToList();
        if (holderIds.Distinct().Count() != holderIds.Count)
        {
            return BadRequest(new { message = "同一产品的份额分配中，持有人不能重复" });
        }

        var existingHolders = await _context.Holders
            .Where(h => holderIds.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();
        var missing = holderIds.Except(existingHolders).ToList();
        if (missing.Any())
        {
            return BadRequest(new { message = $"以下持有人不存在: {string.Join(",", missing)}" });
        }

        // 计算总份额
        var totalAllocation = request.Allocations.Sum(a => a.ShareAmount);
        if (totalAllocation != product.TotalAmount)
        {
            return BadRequest(new { message = $"分配总份额({totalAllocation})必须等于产品总权益({product.TotalAmount})" });
        }

        // 删除旧的份额记录
        _context.HolderShares.RemoveRange(product.HolderShares);

        // 创建新的份额记录
        var now = DateTime.Now;
        foreach (var allocation in request.Allocations)
        {
            var share = new HolderShare
            {
                HolderId = allocation.HolderId,
                ProductId = productId,
                ShareAmount = allocation.ShareAmount,
                InvestmentAmount = allocation.InvestmentAmount ?? allocation.ShareAmount, // 如果没有指定投资金额，默认等于份额
                ShareType = string.IsNullOrWhiteSpace(allocation.ShareType) ? "Subordinate" : allocation.ShareType,
                CreatedAt = now
            };
            _context.HolderShares.Add(share);
        }

        // 更新产品总份额
        product.TotalShares = totalAllocation;
        product.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return Ok(new { message = "份额更新成功" });
    }

    /// <summary>
    /// 清空产品的份额记录（持有人份额 + 份额交易），用于重新分配
    /// </summary>
    [HttpDelete("product/{productId}/clear")]
    public async Task<ActionResult> ClearProductShares(int productId)
    {
        var product = await _context.Products
            .Include(p => p.HolderShares)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        // 删除份额交易记录
        var transactions = await _context.ShareTransactions
            .Where(t => t.ProductId == productId)
            .ToListAsync();
        _context.ShareTransactions.RemoveRange(transactions);

        // 删除持有人份额
        _context.HolderShares.RemoveRange(product.HolderShares);

        // 重置份额数量（保留当前总金额，供重新分配使用）
        product.TotalShares = 0;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { message = "已清空该产品的份额记录，可重新分配" });
    }

    /// <summary>
    /// 产品份额分配概览
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<object>>> GetProductAllocationSummary()
    {
        var products = await _context.Products
            .Include(p => p.HolderShares)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = products.Select(p => new
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            InitialAmount = p.InitialAmount,
            TotalAmount = p.TotalAmount,
            CurrentNetValue = p.CurrentNetValue,
            HolderCount = p.HolderShares.Count,
            TotalShares = p.HolderShares.Sum(hs => hs.ShareAmount),
            Allocated = p.HolderShares.Any()
        });

        return Ok(result);
    }
}

/// <summary>
/// 创建初始份额请求
/// </summary>
public class CreateInitialShareRequest
{
    public int HolderId { get; set; }
    public int ProductId { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal InvestmentAmount { get; set; }
    public string? ShareType { get; set; } // Priority 或 Subordinate
    public DateTime TransactionDate { get; set; } = DateTime.Now;
}

/// <summary>
/// 转让份额请求
/// </summary>
public class TransferShareRequest
{
    public int FromHolderId { get; set; }
    public int ToHolderId { get; set; }
    public int ProductId { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal TransactionPrice { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string? Remarks { get; set; }
}

/// <summary>
/// 新增份额请求
/// </summary>
public class AddShareRequest
{
    public int HolderId { get; set; }
    public int ProductId { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal InvestmentAmount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string? Remarks { get; set; }
}

/// <summary>
/// 减持份额请求
/// </summary>
public class ReduceShareRequest
{
    public int HolderId { get; set; }
    public int ProductId { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal RecoveryAmount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string? Remarks { get; set; }
}

/// <summary>
/// 产品份额分配请求
/// </summary>
public class ProductAllocationRequest
{
    [Required]
    public List<HolderAllocation> Allocations { get; set; } = new();
}

/// <summary>
/// 持有人分配明细
/// </summary>
public class HolderAllocation
{
    public int HolderId { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal? InvestmentAmount { get; set; } // 实际投入资金
    public string? ShareType { get; set; } // Priority / Subordinate
}

