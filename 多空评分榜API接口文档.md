# 多空评分榜API接口文档

## 概述

多空评分榜API提供品种多空评分的读取和写入功能，支持从外部数据源自动刷新榜单数据。API采用RESTful设计，支持JSON格式数据传输。

## 基础信息

- **基础URL**: 根据前端配置确定（通过 `app-config.json` 中的 `rankingsApiUrl` 配置）
- **认证方式**: API Key (仅POST请求需要)
- **数据格式**: JSON
- **字符编码**: UTF-8

## API端点

### 1. 获取榜单数据

#### 接口信息
- **方法**: GET
- **路径**: `/rankings`
- **认证**: 无需认证
- **描述**: 获取当前的多空评分榜数据

#### 请求示例
```bash
GET /rankings
```

#### 响应格式
```json
{
  "longs": [
    {
      "symbol": "RB2505",
      "score": 85.5
    },
    {
      "symbol": "HC2505",
      "score": 78.2
    }
  ],
  "shorts": [
    {
      "symbol": "I2505",
      "score": -72.3
    },
    {
      "symbol": "J2505",
      "score": -68.9
    }
  ]
}
```

#### 响应字段说明

| 字段名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| longs | array | 否 | 多头评分列表 (也可用 `bulls`) |
| shorts | array | 否 | 空头评分列表 (也可用 `bears`) |

##### 评分条目字段说明

| 字段名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| symbol | string | 是 | 合约代码 (也可用 `code`, `instrument`, `InstrumentId`) |
| score | number | 是 | 评分数值 (也可用 `Score`, `value`, `ScoreValue`) |

#### HTTP状态码

| 状态码 | 说明 |
|--------|------|
| 200 | 成功返回榜单数据 |
| 404 | 榜单文件不存在 |
| 500 | 服务器内部错误 |

#### 错误响应示例
```json
{
  "error": "rankings file not found"
}
```

### 2. 保存榜单数据

#### 接口信息
- **方法**: POST
- **路径**: `/rankings`
- **认证**: 需要API Key
- **描述**: 保存多空评分榜数据到服务器

#### 请求头
```
Content-Type: application/json
X-Api-Key: [API_KEY]
```

#### 请求体示例
```json
{
  "longs": [
    {
      "symbol": "RB2505",
      "score": 85.5
    },
    {
      "symbol": "HC2505",
      "score": 78.2
    }
  ],
  "shorts": [
    {
      "symbol": "I2505",
      "score": -72.3
    },
    {
      "symbol": "J2505",
      "score": -68.9
    }
  ]
}
```

#### 响应格式
```json
{
  "saved": true,
  "path": "/app/wwwroot/rankings.json"
}
```

#### HTTP状态码

| 状态码 | 说明 |
|--------|------|
| 200 | 数据保存成功 |
| 401 | API Key无效或缺失 |
| 500 | 服务器内部错误 |

#### 错误响应示例
```json
{
  "error": "invalid api key"
}
```

## 认证说明

### API Key配置

POST请求需要提供有效的API Key：

1. **请求头**: `X-Api-Key: YOUR_API_KEY`
2. **配置位置**: 服务器端 `appsettings.json` 中的 `ApiKeys:RankingsApiKey`
3. **默认值**: `CDef23ddrTTee345dffg445EET#3t354g445g`

### 安全建议

- 生产环境请修改默认API Key
- API Key应定期轮换
- 建议使用HTTPS传输

## 数据格式说明

### 支持的字段别名

API支持多种字段名称以保持兼容性：

**根级字段**:
- `longs` 或 `bulls` (多头列表)
- `shorts` 或 `bears` (空头列表)

**条目字段**:
- `symbol`, `code`, `instrument`, `InstrumentId` (合约代码)
- `score`, `Score`, `value`, `ScoreValue` (评分数值)

### 数据示例

#### 完整数据示例
```json
{
  "longs": [
    {"symbol": "RB2505", "score": 85.5},
    {"symbol": "HC2505", "score": 78.2},
    {"symbol": "J2505", "score": 72.1},
    {"symbol": "JM2505", "score": 68.9},
    {"code": "ZC2505", "Score": 65.4},
    {"instrument": "AP2505", "value": 62.3},
    {"InstrumentId": "CF2505", "ScoreValue": 59.8}
  ],
  "shorts": [
    {"symbol": "I2505", "score": -72.3},
    {"symbol": "P2505", "score": -68.9},
    {"symbol": "M2505", "score": -65.4},
    {"symbol": "RM2505", "score": -62.1},
    {"symbol": "OI2505", "score": -58.7}
  ]
}
```

#### 最小数据示例
```json
{
  "bulls": [
    {"symbol": "RB2505", "score": 85.5}
  ],
  "bears": [
    {"symbol": "I2505", "score": -72.3}
  ]
}
```

## 自动刷新功能

### 配置说明

服务器支持自动从外部URL获取榜单数据并更新本地文件：

**配置文件位置**: `appsettings.json`

```json
{
  "RankingsSettings": {
    "AutoRefreshUrl": "https://api.example.com/rankings",
    "AutoRefreshHour": 12
  }
}
```

### 配置字段说明

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| AutoRefreshUrl | string | 空 | 外部数据源URL，为空时禁用自动刷新 |
| AutoRefreshHour | number | 3 | 每日刷新时间（24小时制） |

### 刷新机制

1. **定时任务**: 每天在指定时间自动执行
2. **首次运行**: 服务启动时立即执行一次刷新
3. **错误处理**: 刷新失败时记录日志，不影响服务运行
4. **数据验证**: 下载的数据会进行JSON格式验证

### 日志示例
```
[RankingsRefreshService] scheduled first refresh at 2024-01-19 12:00:00
[RankingsRefreshService] fetching rankings from https://api.example.com/rankings
[RankingsRefreshService] rankings refreshed and saved to /app/wwwroot/rankings.json
```

## 前端配置

### 配置文件位置
`wwwroot/app-config.json`

```json
{
  "rankingsApiUrl": "http://your-server.com"
}
```

### 配置说明

- **rankingsApiUrl**: 榜单API服务器地址
- 如果未配置或为空，将使用相对路径（同一域名）
- 支持HTTPS协议

## 错误处理

### 常见错误码

| 错误码 | 说明 | 解决方法 |
|--------|------|----------|
| 401 Unauthorized | API Key无效 | 检查X-Api-Key头部 |
| 404 Not Found | 榜单文件不存在 | 首次使用前先POST数据 |
| 500 Internal Server Error | 服务器错误 | 检查服务器日志 |

### 客户端错误处理

前端代码包含完善的错误处理：

```javascript
try {
    const response = await fetch('/rankings');
    if (!response.ok) throw new Error('API请求失败');
    const data = await response.json();
    renderRankings(data);
} catch (error) {
    console.error('加载榜单失败:', error);
    showErrorMessage('无法获取榜单数据');
}
```

## 测试示例

### 使用curl测试

```bash
# 获取榜单数据
curl -X GET "http://localhost:5000/rankings"

# 保存榜单数据
curl -X POST "http://localhost:5000/rankings" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: CDef23ddrTTee345dffg445EET#3t354g445g" \
  -d '{
    "longs": [{"symbol": "RB2505", "score": 85.5}],
    "shorts": [{"symbol": "I2505", "score": -72.3}]
  }'
```

### 使用PowerShell测试

```powershell
# 获取榜单数据
Invoke-RestMethod -Uri "http://localhost:5000/rankings" -Method GET

# 保存榜单数据
$body = @{
    longs = @(@{symbol="RB2505"; score=85.5})
    shorts = @(@{symbol="I2505"; score=-72.3})
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/rankings" -Method POST `
  -Headers @{ "X-Api-Key" = "CDef23ddrTTee345dffg445EET#3t354g445g" } `
  -Body $body -ContentType "application/json"
```

## 部署注意事项

1. **文件权限**: 确保应用有写入 `wwwroot` 目录的权限
2. **HTTPS**: 生产环境建议使用HTTPS
3. **API Key**: 生产环境必须修改默认API Key
4. **备份**: 重要数据建议定期备份
5. **监控**: 建议监控自动刷新服务的日志

## 更新历史

- **v1.0**: 初始版本，支持基本的GET/POST操作
- **v1.1**: 添加自动刷新功能
- **v1.2**: 支持多种字段别名，提高兼容性
- **v1.3**: 添加前端服务器地址配置支持
