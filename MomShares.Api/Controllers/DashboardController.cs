using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 仪表盘控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminAuthorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDashboardStats()
    {
        // 正在运行的产品数（所有产品）
        var activeProductsCount = await _context.Products.CountAsync();

        // 累计产品权益（所有产品的当前总金额之和）
        // SQLite 不支持 decimal 类型的 Sum，需要先加载到内存再聚合
        var products = await _context.Products.Select(p => p.TotalAmount).ToListAsync();
        var totalProductAmount = products.Sum();

        // 累计分红金额（所有分红记录的总金额）
        // SQLite 不支持 decimal 类型的 Sum，需要先加载到内存再聚合
        var dividends = await _context.Dividends.Select(d => d.TotalAmount).ToListAsync();
        var totalDividendAmount = dividends.Sum();

        // 分红笔数
        var dividendCount = await _context.Dividends.CountAsync();

        // 投资者人数（去重后的持有者数量）
        var investorCount = await _context.Holders.CountAsync();

        // 当日盈利（今天所有产品的总金额变化，需要计算净值变化带来的收益）
        var today = DateTime.Today;
        var todayNetValues = await _context.ProductNetValues
            .Where(nv => nv.NetValueDate.Date == today)
            .Include(nv => nv.Product)
            .ToListAsync();

        // 计算当日盈利：当日净值变化 * 产品份额
        decimal todayProfit = 0;
        foreach (var nv in todayNetValues)
        {
            // 获取前一个交易日的净值
            var previousNetValue = await _context.ProductNetValues
                .Where(pnv => pnv.ProductId == nv.ProductId && pnv.NetValueDate < today)
                .OrderByDescending(pnv => pnv.NetValueDate)
                .FirstOrDefaultAsync();

            if (previousNetValue != null)
            {
                var netValueChange = nv.NetValue - previousNetValue.NetValue;
                var product = await _context.Products.FindAsync(nv.ProductId);
                if (product != null)
                {
                    // 盈利 = (当前净值 - 前一日净值) * 总份额
                    todayProfit += netValueChange * product.TotalShares;
                }
            }
        }

        return Ok(new
        {
            ActiveProductsCount = activeProductsCount,
            TotalProductAmount = totalProductAmount,
            TotalDividendAmount = totalDividendAmount,
            DividendCount = dividendCount,
            InvestorCount = investorCount,
            TodayProfit = todayProfit
        });
    }

    /// <summary>
    /// 获取最近10周总产品权益变化数据
    /// </summary>
    [HttpGet("weekly-amounts")]
    public async Task<ActionResult<IEnumerable<object>>> GetWeeklyAmounts()
    {
        var today = DateTime.Today;
        var weeks = new List<object>();

        // 获取所有产品
        var products = await _context.Products.ToListAsync();

        // 获取最近10周的数据
        for (int i = 9; i >= 0; i--)
        {
            var weekStart = today.AddDays(-(i * 7 + (int)today.DayOfWeek));
            var weekEnd = weekStart.AddDays(6);

            decimal weekTotalAmount = 0;

            // 对每个产品，获取该周最后一天的净值，计算权益
            foreach (var product in products)
            {
                // 获取该周最后一天的净值记录
                var weekNetValue = await _context.ProductNetValues
                    .Where(nv => nv.ProductId == product.Id && 
                                 nv.NetValueDate >= weekStart && 
                                 nv.NetValueDate <= weekEnd)
                    .OrderByDescending(nv => nv.NetValueDate)
                    .FirstOrDefaultAsync();

                if (weekNetValue != null)
                {
                    // 使用该周的净值计算权益：净值 * 总份额
                    weekTotalAmount += weekNetValue.NetValue * product.TotalShares;
                }
                else
                {
                    // 如果没有该周的净值记录，尝试获取该周之前最近的净值
                    var previousNetValue = await _context.ProductNetValues
                        .Where(nv => nv.ProductId == product.Id && nv.NetValueDate < weekStart)
                        .OrderByDescending(nv => nv.NetValueDate)
                        .FirstOrDefaultAsync();

                    if (previousNetValue != null)
                    {
                        weekTotalAmount += previousNetValue.NetValue * product.TotalShares;
                    }
                    else
                    {
                        // 如果完全没有净值记录，使用产品当前总金额
                        weekTotalAmount += product.TotalAmount;
                    }
                }
            }

            // 如果没有产品，使用当前总金额
            if (weekTotalAmount == 0 && products.Count > 0)
            {
                // 使用 LINQ to Objects 的 Sum（在内存中计算）
                weekTotalAmount = products.Select(p => p.TotalAmount).Sum();
            }

            weeks.Add(new
            {
                Week = weekStart.ToString("yyyy-MM-dd"),
                WeekLabel = $"第{10 - i}周",
                Amount = weekTotalAmount
            });
        }

        return Ok(weeks);
    }
}

