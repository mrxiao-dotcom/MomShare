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

        // 累计产品权益（所有产品的当前权益之和）
        // 使用 当前净值 * 总份额 来计算，而不是直接使用 TotalAmount（可能不准确）
        // SQLite 不支持 decimal 类型的 Sum，需要先加载到内存再聚合
        var products = await _context.Products.ToListAsync();
        var totalProductAmount = products.Sum(p => p.CurrentNetValue * p.TotalShares);

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
    /// 获取最近10周总产品权益变化数据（基于历史记录）
    /// </summary>
    [HttpGet("weekly-amounts")]
    public async Task<ActionResult<IEnumerable<object>>> GetWeeklyAmounts()
    {
        var today = DateTime.Today;
        var weeks = new List<object>();

        // 获取所有历史权益记录，按日期排序
        var allEquityRecords = await _context.DailyTotalEquities
            .OrderBy(e => e.RecordDate)
            .ToListAsync();

        // 如果没有历史记录，返回空数组（只显示第一周）
        if (allEquityRecords.Count == 0)
        {
            return Ok(new List<object>());
        }

        // 获取最早和最晚的记录日期
        var earliestDate = allEquityRecords.First().RecordDate.Date;
        var latestDate = allEquityRecords.Last().RecordDate.Date;

        // 计算需要显示的周数（最多10周，从最早记录开始）
        var totalDays = (latestDate - earliestDate).Days;
        var totalWeeks = Math.Min(10, (int)Math.Ceiling(totalDays / 7.0) + 1);

        // 如果总周数少于10周，只显示有数据的周
        var weeksToShow = Math.Min(10, totalWeeks);

        // 从最早记录开始，按周分组
        for (int i = 0; i < weeksToShow; i++)
        {
            var weekStart = earliestDate.AddDays(i * 7 - (int)earliestDate.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            // 获取该周最后一天的权益记录
            var weekEquity = allEquityRecords
                .Where(e => e.RecordDate.Date >= weekStart && e.RecordDate.Date <= weekEnd)
                .OrderByDescending(e => e.RecordDate)
                .FirstOrDefault();

            // 如果该周没有记录，尝试使用该周之前最近的记录
            if (weekEquity == null)
            {
                weekEquity = allEquityRecords
                    .Where(e => e.RecordDate.Date < weekStart)
                    .OrderByDescending(e => e.RecordDate)
                    .FirstOrDefault();
            }

            // 如果仍然没有记录，使用当前权益（所有产品的当前净值 * 总份额）
            decimal weekTotalAmount = 0;
            if (weekEquity != null)
            {
                weekTotalAmount = weekEquity.TotalAmount;
            }
            else
            {
                var products = await _context.Products.ToListAsync();
                weekTotalAmount = products.Sum(p => p.CurrentNetValue * p.TotalShares);
            }

            weeks.Add(new
            {
                Week = weekStart.ToString("yyyy-MM-dd"),
                WeekLabel = $"第{i + 1}周",
                Amount = weekTotalAmount
            });
        }

        return Ok(weeks);
    }
}

