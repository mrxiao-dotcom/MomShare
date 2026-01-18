using System;
using System.IO;
using System.Threading.Tasks;
using MomShares.Api;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("开始测试Web服务器...");

        try
        {
            // 创建Web应用
            Console.WriteLine("创建Web应用...");
            var app = WebAppBuilder.CreateWebApplication(null, "http://localhost:5001");

            // 初始化数据库
            Console.WriteLine("初始化数据库...");
            await WebAppBuilder.InitializeDatabaseAsync(app);

            Console.WriteLine("启动Web服务器...");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}
