# API访问说明

## 启动API服务

### 方式1：在Visual Studio中运行
1. 右键点击 `MomShares.Api` 项目
2. 选择"设为启动项目"
3. 按 F5 运行
4. 浏览器会自动打开 Swagger UI

### 方式2：使用WPF控制端启动
1. 运行 `MomShares.Server` 项目
2. 点击"启动服务"按钮
3. 服务将在配置的端口启动（默认5000）

### 方式3：命令行启动
```bash
cd MomShares.Api
dotnet run
```

## 访问地址

### Swagger API文档
- **开发环境**：http://localhost:5000/swagger
- **HTTPS**：https://localhost:5001/swagger

### API端点
- **基础URL**：http://localhost:5000/api
- **健康检查**：http://localhost:5000/api/health

## 默认账号

- **管理员**：用户名 `admin`，密码 `admin123`

## 常见问题

### 1. 找不到网页（404错误）
- 检查服务是否已启动
- 确认端口是否正确（默认5000）
- 尝试访问 http://localhost:5000/swagger

### 2. 端口被占用
- 修改 `launchSettings.json` 中的端口号
- 或修改 WPF 控制端中的端口配置

### 3. 数据库未创建
- 首次运行会自动创建数据库 `MomShares.db`
- 如果数据库文件不存在，会在项目根目录创建

### 4. 无法访问Swagger
- 确保在开发环境运行（ASPNETCORE_ENVIRONMENT=Development）
- 或检查 Program.cs 中 Swagger 配置

## API测试步骤

1. **启动服务**后，访问 http://localhost:5000/swagger
2. **登录获取Token**：
   - 使用 `/api/auth/admin/login` 接口
   - 用户名：admin，密码：admin123
   - 复制返回的 Token
3. **在Swagger中授权**：
   - 点击右上角的 "Authorize" 按钮
   - 输入：`Bearer {你的Token}`
   - 点击 "Authorize"
4. **测试其他接口**：
   - 现在可以测试需要认证的接口了

