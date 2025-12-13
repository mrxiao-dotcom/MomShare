using MomShares.Api;

// 创建Web应用
var app = WebAppBuilder.CreateWebApplication(args);

// 初始化数据库
await WebAppBuilder.InitializeDatabaseAsync(app);

// 显示友好的启动信息
var urls = app.Urls;
Console.WriteLine("========================================");
Console.WriteLine("  份额管理系统 - Web服务器");
Console.WriteLine("========================================");
Console.WriteLine($"  服务已启动！");
if (urls.Any())
{
    foreach (var url in urls)
    {
        Console.WriteLine($"  管理界面: {url}");
        Console.WriteLine($"  API文档: {url}/swagger");
    }
}
else
{
    Console.WriteLine($"  监听地址: http://localhost:5000");
    Console.WriteLine($"  管理界面: http://localhost:5000");
    Console.WriteLine($"  API文档: http://localhost:5000/swagger");
}
Console.WriteLine("========================================");
Console.WriteLine("  按 Ctrl+C 停止服务");
Console.WriteLine("========================================");

app.Run();
