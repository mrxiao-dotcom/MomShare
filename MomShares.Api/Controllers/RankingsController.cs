using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace MomShares.Api.Controllers;

/// <summary>
/// 提供品种多空评分榜的读写接口和代理功能
/// GET  /rankings  -> 返回本地 wwwroot/rankings.json 内容（如果存在）
/// POST /rankings  -> 接受 JSON 数据并保存到 wwwroot/rankings.json
/// GET  /rankings/proxy  -> 代理请求外部榜单API，避免CORS问题
/// </summary>

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

    [HttpGet("proxy")]
    public async Task<IActionResult> GetRankingsProxy()
    {
        try
        {
            // 从配置中获取外部API地址
            var config = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var externalUrl = config?["RankingsSettings:AutoRefreshUrl"] ?? "";

            if (string.IsNullOrWhiteSpace(externalUrl))
            {
                return BadRequest("external rankings API URL not configured");
            }

            // 使用HttpClient请求外部API
            var httpClient = HttpContext.RequestServices.GetService<System.Net.Http.IHttpClientFactory>()?.CreateClient();
            if (httpClient == null)
            {
                return StatusCode(500, "HTTP client not available");
            }

            var response = await httpClient.GetAsync(externalUrl);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, $"external API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();

            // 检查内容类型
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            // 如果是JSON，验证格式；如果是HTML或其他，直接返回
            if (contentType.Contains("json") || content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    return Content(content, "application/json");
                }
                catch (JsonException ex)
                {
                    return BadRequest($"invalid JSON from external API: {ex.Message}");
                }
            }
            else
            {
                // 如果不是JSON，直接返回内容（可能是HTML或其他格式）
                return Content(content, contentType);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"proxy request failed: {ex.Message}");
        }
    }
}


