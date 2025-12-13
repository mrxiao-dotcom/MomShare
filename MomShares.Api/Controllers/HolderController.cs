using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Infrastructure.Data;
using System.Security.Claims;

namespace MomShares.Api.Controllers;

/// <summary>
/// 持有者端控制器（持有者查看自己的信息）
/// </summary>
[ApiController]
[Route("api/holder")]
[HolderAuthorize]
public class HolderController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HolderController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取当前持有者信息
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<object>> GetHolderInfo()
    {
        var holderId = GetCurrentHolderId();
        if (holderId == null)
        {
            return Unauthorized();
        }

        var holder = await _context.Holders
            .FirstOrDefaultAsync(h => h.Id == holderId.Value);

        if (holder == null)
        {
            return NotFound(new { message = "持有者不存在" });
        }

        return Ok(new
        {
            id = holder.Id,
            name = holder.Name,
            phone = holder.Phone,
            phoneNumber = holder.PhoneNumber,
            email = holder.Email,
            bankName = holder.BankName,
            bankAccount = holder.BankAccount,
            accountName = holder.AccountName
        });
    }

    /// <summary>
    /// 获取我的份额列表
    /// </summary>
    [HttpGet("shares")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyShares()
    {
        var holderId = GetCurrentHolderId();
        if (holderId == null)
        {
            return Unauthorized();
        }

        var shares = await _context.HolderShares
            .Include(hs => hs.Product)
                .ThenInclude(p => p.DistributionPlan)
            .Include(hs => hs.Product)
                .ThenInclude(p => p.HolderShares)
            .Where(hs => hs.HolderId == holderId.Value)
            .ToListAsync();

        var result = new List<object>();
        
        foreach (var hs in shares)
        {
            var product = hs.Product!;
            var currentValue = hs.ShareAmount * product.CurrentNetValue;
            var totalShares = product.TotalShares > 0 ? product.TotalShares : 1;
            var shareRatio = hs.ShareAmount / totalShares;
            
            // 计算当前权益（当前净值 * 总份额）
            var currentTotalAmount = product.CurrentNetValue * totalShares;
            
            // 计算产品总盈利/亏损（当前权益 - 产品初始权益）
            var productBalance = currentTotalAmount - product.InitialAmount;
            
            // 获取劣后分配比例
            var subordinateRatio = product.DistributionPlan?.SubordinateRatio ?? 40m;
            
            // 计算预期分红和权益残值
            decimal expectedDividend = 0;
            decimal residualValue = 0;
            decimal holderBalance = 0; // 持有人结余
            
            if (productBalance > 0)
            {
                // 盈利情况：分红金额 = 产品盈利 * 劣后分配比例 * 份额占比
                // 需要计算该持有者在劣后方中的占比
                var subordinateShares = product.HolderShares
                    .Where(s => s.ShareType == "Subordinate" || string.IsNullOrEmpty(s.ShareType))
                    .Sum(s => s.ShareAmount);
                
                var isSubordinate = hs.ShareType == "Subordinate" || string.IsNullOrEmpty(hs.ShareType);
                var subordinateRatioInSub = subordinateShares > 0 && isSubordinate
                    ? hs.ShareAmount / subordinateShares
                    : 0;
                
                // 如果持有者是劣后方，才参与分红计算
                if (isSubordinate)
                {
                    expectedDividend = productBalance * (subordinateRatio / 100m) * subordinateRatioInSub;
                }
                else
                {
                    // 优先方不参与分红计算（根据之前的业务逻辑）
                    expectedDividend = 0;
                }
                
                // 持有人结余 = 初始投入 + 预期分红
                holderBalance = hs.InvestmentAmount + expectedDividend;
                residualValue = holderBalance;
            }
            else
            {
                // 亏损情况：亏损 = 亏损金额 * 份额占比
                var loss = Math.Abs(productBalance) * shareRatio;
                // 持有人结余 = 初始投入 - 亏损 * 份额比例
                holderBalance = Math.Max(0, hs.InvestmentAmount - loss);
                residualValue = holderBalance;
                expectedDividend = 0;
            }
            
            // 计算投资回报率
            var returnRate = hs.InvestmentAmount > 0 
                ? ((residualValue - hs.InvestmentAmount) / hs.InvestmentAmount) * 100 
                : 0;
            
            result.Add(new
            {
                id = hs.Id,
                productId = hs.ProductId,
                productName = product.Name,
                productCode = product.Code,
                shareAmount = hs.ShareAmount,
                investmentAmount = hs.InvestmentAmount,
                currentNetValue = product.CurrentNetValue,
                currentValue = currentValue,
                shareRatio = shareRatio,
                balance = holderBalance, // 持有人结余
                expectedDividend = expectedDividend,
                residualValue = residualValue,
                returnRate = returnRate,
                shareType = hs.ShareType ?? "Subordinate"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取产品净值走势
    /// </summary>
    [HttpGet("products/{productId}/netvalues")]
    public async Task<ActionResult<IEnumerable<object>>> GetProductNetValues(int productId)
    {
        var holderId = GetCurrentHolderId();
        if (holderId == null)
        {
            return Unauthorized();
        }

        // 验证持有者是否持有该产品
        var hasShare = await _context.HolderShares
            .AnyAsync(hs => hs.HolderId == holderId.Value && hs.ProductId == productId);

        if (!hasShare)
        {
            return Forbid("您没有持有该产品");
        }

        var netValues = await _context.ProductNetValues
            .Where(nv => nv.ProductId == productId)
            .OrderBy(nv => nv.NetValueDate)
            .Select(nv => new
            {
                date = nv.NetValueDate.ToString("yyyy-MM-dd"),
                value = nv.NetValue
            })
            .ToListAsync();

        return Ok(netValues);
    }

    /// <summary>
    /// 获取我的分红记录
    /// </summary>
    [HttpGet("dividends")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyDividends()
    {
        var holderId = GetCurrentHolderId();
        if (holderId == null)
        {
            return Unauthorized();
        }

        var dividends = await _context.DividendDetails
            .Include(dd => dd.Dividend)
                .ThenInclude(d => d!.Product)
            .Where(dd => dd.HolderId == holderId.Value)
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

        return Ok(dividends);
    }

    /// <summary>
    /// 获取当前持有者ID
    /// </summary>
    private int? GetCurrentHolderId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var userTypeClaim = User.FindFirst("UserType");
        if (userTypeClaim?.Value != "Holder")
        {
            return null;
        }

        return userId;
    }
}

