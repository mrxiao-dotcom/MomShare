using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MomShares.Api.Controllers;

/// <summary>
/// 提供品种多空评分榜的读写接口
/// GET  /rankings  -> 返回本地 wwwroot/rankings.json 内容（如果存在）
/// POST /rankings  -> 接受 JSON 数据并保存到 wwwroot/rankings.json
/// </summary>
[ApiController]
[Route("rankings")]
[AllowAnonymous]
public class RankingsController : ControllerBase
{
    private static string GetRankingsPath()
    {
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        return Path.Combine(wwwroot, "rankings.json");
    }

    [HttpGet]
    public IActionResult GetRankings()
    {
        try
        {
            var path = GetRankingsPath();
            if (!System.IO.File.Exists(path))
            {
                return NotFound("rankings file not found");
            }
            var json = System.IO.File.ReadAllText(path);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"read rankings failed: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveRankings([FromBody] JsonElement payload)
    {
        try
        {
            // 验证 API Key
            var config = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var expectedKey = config?["ApiKeys:RankingsApiKey"] ?? "";
            var providedKey = "";
            if (Request.Headers.TryGetValue("X-Api-Key", out var headerVals))
            {
                providedKey = headerVals.FirstOrDefault() ?? "";
            }
            if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            {
                return Unauthorized("invalid api key");
            }

            var path = GetRankingsPath();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(payload, options);
            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await System.IO.File.WriteAllTextAsync(path, json);
            return Ok(new { saved = true, path });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"save rankings failed: {ex.Message}");
        }
    }
}


