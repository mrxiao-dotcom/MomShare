using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MomShares.Api.Controllers;

/// <summary>
/// 期货工具箱控制器 - 无需认证的独立功能
/// </summary>
[ApiController]
[Route("futures")]
[AllowAnonymous] // 明确允许匿名访问
public class FuturesController : ControllerBase
{
    /// <summary>
    /// 获取期货工具箱页面
    /// </summary>
    [HttpGet]
    [Produces("text/html")]
    public IActionResult GetFuturesToolbox()
    {
        try
        {
            Console.WriteLine($"[FuturesController] 开始处理 /futures 请求");

            // 使用环境配置的WebRootPath
            var webHostEnv = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var wwwrootPath = webHostEnv?.WebRootPath;

            Console.WriteLine($"[FuturesController] WebHostEnvironment: {(webHostEnv == null ? "NULL" : "OK")}");
            Console.WriteLine($"[FuturesController] WebRootPath: {wwwrootPath ?? "NULL"}");

            // 如果没有配置WebRootPath，使用当前目录
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Console.WriteLine($"[FuturesController] 使用默认路径: {wwwrootPath}");
            }

            var htmlPath = Path.Combine(wwwrootPath, "futures-toolbox.html");
            Console.WriteLine($"[FuturesController] 尝试路径1: {htmlPath}, 存在: {System.IO.File.Exists(htmlPath)}");

            // 如果文件不存在，尝试查找API项目目录
            if (!System.IO.File.Exists(htmlPath))
            {
                var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "MomShares.Api", "wwwroot");
                htmlPath = Path.Combine(apiProjectPath, "futures-toolbox.html");
                Console.WriteLine($"[FuturesController] 尝试路径2: {htmlPath}, 存在: {System.IO.File.Exists(htmlPath)}");

                // 如果还是找不到，尝试从执行文件目录查找
                if (!System.IO.File.Exists(htmlPath))
                {
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    Console.WriteLine($"[FuturesController] 程序集位置: {exePath ?? "NULL"}");

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var exeDir = Path.GetDirectoryName(exePath);
                        if (!string.IsNullOrEmpty(exeDir))
                        {
                            htmlPath = Path.Combine(exeDir, "wwwroot", "futures-toolbox.html");
                            Console.WriteLine($"[FuturesController] 尝试路径3: {htmlPath}, 存在: {System.IO.File.Exists(htmlPath)}");
                        }
                    }
                }
            }

            if (!System.IO.File.Exists(htmlPath))
            {
                var errorMsg = $"期货工具箱页面未找到。搜索路径: {wwwrootPath}, {Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "MomShares.Api", "wwwroot")}";
                Console.WriteLine($"[FuturesController] 错误: {errorMsg}");
                return NotFound(errorMsg);
            }

            Console.WriteLine($"[FuturesController] 找到文件: {htmlPath}");

            var htmlContent = System.IO.File.ReadAllText(htmlPath, System.Text.Encoding.UTF8);
            Console.WriteLine($"[FuturesController] 成功读取文件，长度: {htmlContent.Length}");

            return Content(htmlContent, "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FuturesController] 异常: {ex.Message}");
            Console.WriteLine($"[FuturesController] 堆栈跟踪: {ex.StackTrace}");
            return StatusCode(500, $"加载期货工具箱页面时出错: {ex.Message}");
        }
    }
}