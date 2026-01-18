// See https://aka.ms/new-console-template for more information
using System;
using System.Net.Http;
using System.Threading.Tasks;
using MomShares.Api;

// 直接测试端点
try
{
    Console.WriteLine("测试 /futures 端点...");
    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10); // 设置10秒超时

    var response = await client.GetAsync("http://localhost:5000/futures");

    Console.WriteLine($"状态码: {response.StatusCode}");
    Console.WriteLine($"原因: {response.ReasonPhrase}");

    if (!response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"响应内容: {content}");
    }
    else
    {
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"响应长度: {content.Length} 字符");
        Console.WriteLine($"响应前200字符: {content.Substring(0, Math.Min(200, content.Length))}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"请求失败: {ex.Message}");
    Console.WriteLine($"异常类型: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部异常: {ex.InnerException.Message}");
    }
    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
}
