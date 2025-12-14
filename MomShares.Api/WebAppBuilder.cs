using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using MomShares.Infrastructure;
using MomShares.Infrastructure.Data;
using System.Text.Json.Serialization;

namespace MomShares.Api;

/// <summary>
/// Web应用构建器，用于在WPF中承载Web服务器
/// </summary>
public static class WebAppBuilder
{
    /// <summary>
    /// 配置数据库连接字符串
    /// </summary>
    private static string GetDefaultDbPath()
    {
        // 优先检查环境变量
        var envDbPath = Environment.GetEnvironmentVariable("MomShares_DbPath");
        if (!string.IsNullOrEmpty(envDbPath) && File.Exists(envDbPath))
        {
            Console.WriteLine($"[WebAppBuilder] 使用环境变量指定的数据库: {envDbPath}");
            return envDbPath;
        }
        
        // 默认使用 LocalApplicationData（与原有配置保持一致）
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "MomShares");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        var defaultDbPath = Path.Combine(folder, "MomShares.db");
        
        // 如果 AppData 下有数据库，优先使用
        if (File.Exists(defaultDbPath))
        {
            Console.WriteLine($"[WebAppBuilder] 使用 AppData 数据库: {defaultDbPath}");
            return defaultDbPath;
        }
        
        // 检查当前工作目录下的数据库文件（开发环境）
        var currentDir = Directory.GetCurrentDirectory();
        var projectDbPath = Path.Combine(currentDir, "MomShares.db");
        if (File.Exists(projectDbPath))
        {
            Console.WriteLine($"[WebAppBuilder] 使用项目目录数据库: {projectDbPath}");
            return projectDbPath;
        }
        
        // 检查API项目目录下的数据库文件
        var apiDbPath = Path.Combine(currentDir, "..", "..", "..", "..", "MomShares.Api", "MomShares.db");
        apiDbPath = Path.GetFullPath(apiDbPath);
        if (File.Exists(apiDbPath))
        {
            Console.WriteLine($"[WebAppBuilder] 使用API项目目录数据库: {apiDbPath}");
            return apiDbPath;
        }
        
        // 如果都不存在，使用 AppData（会创建新数据库）
        Console.WriteLine($"[WebAppBuilder] 使用默认数据库路径（将创建新数据库）: {defaultDbPath}");
        return defaultDbPath;
    }

    private static string ExpandConnectionString(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs))
        {
            return $"Data Source={GetDefaultDbPath()}";
        }

        // 支持 %LOCALAPPDATA% 占位
        if (cs.Contains("%LOCALAPPDATA%", StringComparison.OrdinalIgnoreCase))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            cs = cs.Replace("%LOCALAPPDATA%", appData, StringComparison.OrdinalIgnoreCase);
        }
        
        // 如果配置的路径存在，直接使用
        if (cs.StartsWith("Data Source="))
        {
            var dbPath = cs.Replace("Data Source=", "");
            if (File.Exists(dbPath))
            {
                Console.WriteLine($"[WebAppBuilder] 使用配置文件指定的数据库: {dbPath}");
                return cs;
            }
            else
            {
                // 如果配置的路径不存在，使用默认路径（优先 AppData）
                var defaultPath = GetDefaultDbPath();
                Console.WriteLine($"[WebAppBuilder] 配置文件中的数据库不存在，使用: {defaultPath}");
                return $"Data Source={defaultPath}";
            }
        }

        return cs;
    }

    /// <summary>
    /// 创建并配置Web应用
    /// </summary>
    public static WebApplication CreateWebApplication(string[]? args = null, string? urls = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

        // 如果指定了URL，设置监听地址
        if (!string.IsNullOrEmpty(urls))
        {
            builder.WebHost.UseUrls(urls);
            Console.WriteLine($"[WebAppBuilder] 设置监听地址: {urls}");
        }
        else
        {
            // 如果没有指定URL，默认监听所有网络接口
            var defaultUrl = "http://0.0.0.0:5000";
            builder.WebHost.UseUrls(defaultUrl);
            Console.WriteLine($"[WebAppBuilder] 使用默认监听地址: {defaultUrl}");
        }

        // 设置内容根目录和Web根目录，确保能找到wwwroot
        var currentDir = Directory.GetCurrentDirectory();
        var wwwrootPath = Path.Combine(currentDir, "wwwroot");
        var contentRoot = currentDir;
        
        // 如果当前目录没有wwwroot，尝试查找API项目的wwwroot
        if (!Directory.Exists(wwwrootPath))
        {
            // 尝试从当前目录向上查找
            var searchDir = currentDir;
            for (int i = 0; i < 5; i++)
            {
                var testPath = Path.Combine(searchDir, "MomShares.Api", "wwwroot");
                if (Directory.Exists(testPath))
                {
                    wwwrootPath = testPath;
                    contentRoot = Path.Combine(searchDir, "MomShares.Api");
                    break;
                }
                searchDir = Path.GetDirectoryName(searchDir) ?? searchDir;
            }
            
            // 如果还是找不到，尝试查找执行文件所在目录
            if (!Directory.Exists(wwwrootPath))
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var exeDir = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(exeDir))
                    {
                        // 尝试执行文件所在目录
                        var testPath = Path.Combine(exeDir, "wwwroot");
                        if (Directory.Exists(testPath))
                        {
                            wwwrootPath = testPath;
                            contentRoot = exeDir;
                        }
                        else
                        {
                            // 尝试执行文件所在目录的父目录（可能是发布目录结构）
                            var parentDir = Path.GetDirectoryName(exeDir);
                            if (!string.IsNullOrEmpty(parentDir))
                            {
                                testPath = Path.Combine(parentDir, "wwwroot");
                                if (Directory.Exists(testPath))
                                {
                                    wwwrootPath = testPath;
                                    contentRoot = parentDir;
                                }
                                else
                                {
                                    // 尝试在父目录下查找 MomShares.Api/wwwroot
                                    testPath = Path.Combine(parentDir, "MomShares.Api", "wwwroot");
                                    if (Directory.Exists(testPath))
                                    {
                                        wwwrootPath = testPath;
                                        contentRoot = Path.Combine(parentDir, "MomShares.Api");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // 设置内容根目录和Web根目录
        builder.Environment.ContentRootPath = contentRoot;
        builder.Environment.WebRootPath = wwwrootPath;
        
        // 记录找到的路径（用于调试）
        Console.WriteLine($"[WebAppBuilder] 当前工作目录: {currentDir}");
        Console.WriteLine($"[WebAppBuilder] 内容根目录: {contentRoot}");
        Console.WriteLine($"[WebAppBuilder] Web根目录: {wwwrootPath}");
        Console.WriteLine($"[WebAppBuilder] wwwroot存在: {Directory.Exists(wwwrootPath)}");
        
        if (Directory.Exists(wwwrootPath))
        {
            var files = Directory.GetFiles(wwwrootPath, "*.*", SearchOption.TopDirectoryOnly);
            Console.WriteLine($"[WebAppBuilder] wwwroot中的文件数: {files.Length}");
            if (files.Length > 0)
            {
                Console.WriteLine($"[WebAppBuilder] 示例文件: {Path.GetFileName(files[0])}");
            }
        }
        else
        {
            Console.WriteLine($"警告: 找不到wwwroot目录，当前目录: {currentDir}");
        }

        // 加载配置文件（优先从API项目目录加载）
        var configPath = Path.Combine(contentRoot, "appsettings.json");
        if (File.Exists(configPath))
        {
            builder.Configuration.AddJsonFile(configPath, optional: true, reloadOnChange: false);
        }
        
        var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var connectionString = ExpandConnectionString(rawConnectionString);
        
        // 记录数据库路径（用于调试）
        Console.WriteLine($"[WebAppBuilder] 数据库连接字符串: {connectionString}");

        // 添加基础设施服务
        builder.Services.AddInfrastructure(connectionString);

        // 配置JWT认证
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "MomShares-Secret-Key-2024-Change-In-Production";
        var issuer = jwtSettings["Issuer"] ?? "MomShares";
        var audience = jwtSettings["Audience"] ?? "MomShares";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // 允许5分钟的时钟偏差，避免服务器时间不同步问题
            };
            
            // 添加事件处理，记录认证错误
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"[JWT认证失败] {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine($"[JWT挑战] {context.Error} - {context.ErrorDescription}");
                    return Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireClaim("UserType", "Admin"));
            options.AddPolicy("HolderOnly", policy => policy.RequireClaim("UserType", "Holder"));
        });

        // 添加控制器（显式指定包含控制器的程序集）
        var controllersAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        builder.Services.AddControllers()
            .AddApplicationPart(controllersAssembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持PascalCase
            });
        
        // 记录控制器程序集信息（用于调试）
        Console.WriteLine($"[WebAppBuilder] 控制器程序集: {controllersAssembly.FullName}");
        Console.WriteLine($"[WebAppBuilder] 控制器程序集位置: {controllersAssembly.Location}");

        // 添加CORS（允许所有来源，用于服务器部署）
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("*"); // 暴露所有响应头
            });
        });

        // 添加Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // 配置HTTP请求管道
        // 启用静态文件服务（用于前端管理页面）
        // 静态文件中间件只处理存在的文件，如果文件不存在，请求会继续到下一个中间件
        if (Directory.Exists(wwwrootPath))
        {
            Console.WriteLine($"[静态文件] 配置静态文件服务，路径: {wwwrootPath}");
            
            // 配置默认文件（index.html）- 必须在 UseStaticFiles 之前
            var defaultFileOptions = new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(wwwrootPath),
                RequestPath = ""
            };
            defaultFileOptions.DefaultFileNames.Clear();
            defaultFileOptions.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(defaultFileOptions);
            
            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wwwrootPath),
                RequestPath = "",
                OnPrepareResponse = context =>
                {
                    // 记录静态文件请求（用于调试）
                    var path = context.Context.Request.Path.Value ?? "";
                    if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".ico"))
                    {
                        Console.WriteLine($"[静态文件] 提供文件: {path}");
                    }
                }
            };
            app.UseStaticFiles(staticFileOptions);
            
            // 验证关键文件是否存在
            var cssPath = Path.Combine(wwwrootPath, "styles.css");
            var jsPath = Path.Combine(wwwrootPath, "app.js");
            Console.WriteLine($"[静态文件] styles.css 存在: {File.Exists(cssPath)}");
            Console.WriteLine($"[静态文件] app.js 存在: {File.Exists(jsPath)}");
        }
        else
        {
            // 如果找不到wwwroot，使用默认配置
            app.UseDefaultFiles();
            app.UseStaticFiles();
            Console.WriteLine($"警告: 找不到wwwroot目录 ({wwwrootPath})，使用默认静态文件配置");
        }

        // 在开发环境或生产环境都启用Swagger（方便测试）
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MomShares API v1");
            c.RoutePrefix = "swagger"; // Swagger UI在 /swagger 路径
        });

        app.UseCors("AllowAll");

        // 仅在非开发环境启用 HTTPS 重定向，避免开发证书警告
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // 启用路由（明确路由边界）
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        
        // 先映射API路由
        app.MapControllers();
        
        // 记录已注册的路由（用于调试）
        // 在应用启动后，可以通过端点数据源查看所有注册的路由
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
            var endpoints = endpointDataSource.Endpoints;
            Console.WriteLine($"[WebAppBuilder] 已注册 {endpoints.Count} 个端点");
            foreach (var endpoint in endpoints.OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>())
            {
                Console.WriteLine($"[WebAppBuilder] 路由: {endpoint.RoutePattern.RawText} -> {endpoint.DisplayName}");
            }
        });
        
        // 配置默认路由到前端页面（只对非API路由生效）
        // MapFallback 只在所有路由都不匹配时才执行
        // 如果 API 路径没有被匹配（比如认证失败），应该返回 404 而不是 index.html
        app.MapFallback(async context =>
        {
            // 记录请求路径（用于调试）
            var requestPath = context.Request.Path.Value ?? "/";
            Console.WriteLine($"[MapFallback] 处理请求: {context.Request.Method} {requestPath}");
            
            // 如果是 API 或 Swagger 路径，返回 404（不应该到达这里，但以防万一）
            if (context.Request.Path.StartsWithSegments("/api") || 
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                Console.WriteLine($"[MapFallback] API/Swagger 路径，返回 404");
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("API endpoint not found");
                return;
            }
            
            // 否则，如果wwwroot存在，读取并返回index.html
            Console.WriteLine($"[MapFallback] 检查 wwwroot: {wwwrootPath}");
            Console.WriteLine($"[MapFallback] wwwroot 存在: {Directory.Exists(wwwrootPath)}");
            
            if (Directory.Exists(wwwrootPath))
            {
                var indexPath = Path.Combine(wwwrootPath, "index.html");
                Console.WriteLine($"[MapFallback] index.html 路径: {indexPath}");
                Console.WriteLine($"[MapFallback] index.html 存在: {File.Exists(indexPath)}");
                
                if (File.Exists(indexPath))
                {
                    Console.WriteLine($"[MapFallback] 返回 index.html");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(indexPath);
                    return;
                }
                else
                {
                    Console.WriteLine($"[MapFallback] 错误: index.html 文件不存在");
                }
            }
            else
            {
                Console.WriteLine($"[MapFallback] 错误: wwwroot 目录不存在");
            }
            
            Console.WriteLine($"[MapFallback] 返回 404");
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"Page not found. wwwroot: {wwwrootPath}, exists: {Directory.Exists(wwwrootPath)}");
        });

        return app;
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<MomShares.Core.Interfaces.IPasswordService>();
            var initializer = new MomShares.Infrastructure.Services.DatabaseInitializer(dbContext, passwordService);
            await initializer.InitializeAsync();
        }
    }
}

