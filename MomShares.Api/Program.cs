using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MomShares.Infrastructure;
using MomShares.Infrastructure.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库连接：默认放在 AppData\Local\MomShares\MomShares.db
string GetDefaultDbPath()
{
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var folder = Path.Combine(appData, "MomShares");
    if (!Directory.Exists(folder))
    {
        Directory.CreateDirectory(folder);
    }
    return Path.Combine(folder, "MomShares.db");
}

string ExpandConnectionString(string? cs)
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

    return cs;
}

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = ExpandConnectionString(rawConnectionString);

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
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("UserType", "Admin"));
    options.AddPolicy("HolderOnly", policy => policy.RequireClaim("UserType", "Holder"));
});

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持PascalCase
    });

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<MomShares.Core.Interfaces.IPasswordService>();
    var initializer = new MomShares.Infrastructure.Services.DatabaseInitializer(dbContext, passwordService);
    await initializer.InitializeAsync();
}

// 配置HTTP请求管道
// 启用静态文件服务（用于前端管理页面）
app.UseStaticFiles();

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 配置默认路由到前端页面
app.MapFallbackToFile("index.html");

app.Run();
