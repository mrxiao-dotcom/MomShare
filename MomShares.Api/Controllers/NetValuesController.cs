using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomShares.Api.Filters;
using MomShares.Core.Entities;
using MomShares.Infrastructure.Data;

namespace MomShares.Api.Controllers;

/// <summary>
/// 净值管理控制器
/// </summary>
[ApiController]
[Route("api/products/{productId}/[controller]")]
[AdminAuthorize]
public class NetValuesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NetValuesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取产品净值历史记录
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductNetValue>>> GetNetValues(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        return await _context.ProductNetValues
            .Where(nv => nv.ProductId == productId)
            .OrderByDescending(nv => nv.NetValueDate)
            .ToListAsync();
    }

    /// <summary>
    /// 获取净值走势数据（用于图表）
    /// </summary>
    [HttpGet("chart")]
    public async Task<ActionResult<IEnumerable<object>>> GetNetValueChart(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
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
    /// 录入净值
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductNetValue>> CreateNetValue(int productId, [FromBody] CreateNetValueRequest request)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        var netValue = new ProductNetValue
        {
            ProductId = productId,
            NetValueDate = request.NetValueDate,
            NetValue = request.NetValue,
            CreatedAt = DateTime.Now
        };

        _context.ProductNetValues.Add(netValue);

        // 更新产品当前净值
        product.CurrentNetValue = request.NetValue;
        // 同步更新产品总金额（当前净值 * 总份额）
        product.TotalAmount = request.NetValue * product.TotalShares;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetNetValues), new { productId }, netValue);
    }

    /// <summary>
    /// 删除净值记录
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNetValue(int productId, int id)
    {
        var netValue = await _context.ProductNetValues.FirstOrDefaultAsync(nv => nv.Id == id && nv.ProductId == productId);
        if (netValue == null)
        {
            return NotFound(new { message = "净值记录不存在" });
        }

        _context.ProductNetValues.Remove(netValue);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 批量导入净值
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult> BatchImportNetValues(int productId, [FromBody] List<CreateNetValueRequest> requests)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = "产品不存在" });
        }

        if (requests == null || requests.Count == 0)
        {
            return BadRequest(new { message = "导入数据不能为空" });
        }

        var netValues = requests.Select(r => new ProductNetValue
        {
            ProductId = productId,
            NetValueDate = r.NetValueDate,
            NetValue = r.NetValue,
            CreatedAt = DateTime.Now
        }).ToList();

        _context.ProductNetValues.AddRange(netValues);

        // 更新产品当前净值为最新的净值
        var latestNetValue = requests.OrderByDescending(r => r.NetValueDate).First();
        product.CurrentNetValue = latestNetValue.NetValue;
        // 同步更新产品总金额（当前净值 * 总份额）
        product.TotalAmount = latestNetValue.NetValue * product.TotalShares;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { message = $"成功导入 {netValues.Count} 条净值记录" });
    }
}

/// <summary>
/// 创建净值请求
/// </summary>
public class CreateNetValueRequest
{
    public DateTime NetValueDate { get; set; }
    public decimal NetValue { get; set; }
}

