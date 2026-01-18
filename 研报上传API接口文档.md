# 研报（策略）上传API接口文档

## 📋 概述

研报管理系统基于策略（Strategy）模型，支持期货和加密货币策略的创建、更新和管理。本系统将"研报"等同于"策略"，支持通过API接口上传HTML格式的研报内容。

## 🔗 基础信息

- **基础URL**: `http://your-server:8080`
- **API路径**: `/api/strategies`
- **认证方式**: 无需认证（内部管理系统）
- **数据格式**: JSON
- **字符编码**: UTF-8

## 📚 API接口列表

### 1. 获取策略列表

#### 接口信息
- **方法**: GET
- **路径**: `/api/strategies/list`
- **描述**: 获取所有策略的列表，支持按类别筛选

#### 请求参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| category | int | 否 | 策略类别（1=期货，2=加密货币） |
| includeDisabled | bool | 否 | 是否包含禁用的策略，默认false |

#### 请求示例
```bash
# 获取所有策略
GET /api/strategies/list

# 获取期货策略（包含禁用的）
GET /api/strategies/list?category=1&includeDisabled=true
```

#### 响应格式
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "category": "Futures",
      "categoryId": 1,
      "title": "期货策略分析报告",
      "summary": "{\"keyPoints\":[\"技术指标\",\"市场趋势\"],\"riskLevel\":\"中\"}",
      "isEnabled": true,
      "createdAt": "2024-01-15T10:30:00",
      "updatedAt": "2024-01-15T14:20:00",
      "remarks": "期货市场分析"
    }
  ]
}
```

### 2. 获取研报摘要

#### 接口信息
- **方法**: GET
- **路径**: `/api/strategies/{id}/summary`
- **描述**: 获取指定策略的摘要JSON数据

#### 路径参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| id | int | 是 | 策略ID |

#### 请求示例
```bash
GET /api/strategies/123/summary
```

#### 响应格式
直接返回JSON摘要数据，Content-Type: application/json

**示例摘要JSON结构：**
```json
{
  "keyPoints": [
    "技术分析指标",
    "市场趋势预测",
    "风险评估"
  ],
  "riskLevel": "中",
  "targetPrice": {
    "support": 3500,
    "resistance": 3800
  },
  "timeframe": "1-3个月",
  "confidence": 75
}
```

### 3. 上传新研报（创建策略）

#### 接口信息
- **方法**: POST
- **路径**: `/api/strategies`
- **描述**: 创建新的策略研报

#### 请求头
```
Content-Type: application/json
```

#### 请求参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| categoryId | int | 是 | 策略类别ID（1=期货，2=加密货币） |
| title | string | 是 | 研报标题 |
| htmlContent | string | 是 | HTML格式的研报内容 |
| summary | string | 否 | 摘要数据（JSON格式） |
| isEnabled | bool | 否 | 是否启用，默认true |
| remarks | string | 否 | 备注信息 |

#### 请求示例
```bash
POST /api/strategies
Content-Type: application/json

{
  "categoryId": 1,
  "title": "2024年期货市场研报",
  "htmlContent": "<h1>期货市场分析</h1><p>详细的市场分析内容...</p>",
  "summary": "{\"keyPoints\":[\"技术指标\",\"市场趋势\"],\"riskLevel\":\"中\"}",
  "isEnabled": true,
  "remarks": "2024年第一季度研报"
}
```

#### 响应格式
```json
{
  "success": true,
  "message": "策略创建成功",
  "data": {
    "id": 123,
    "category": "Futures",
    "categoryId": 1,
    "title": "2024年期货市场研报",
    "summary": "{\"keyPoints\":[\"技术指标\",\"市场趋势\"],\"riskLevel\":\"中\"}",
    "isEnabled": true,
    "createdAt": "2024-01-15T15:30:00"
  }
}
```

### 3. 更新研报（更新策略）

#### 接口信息
- **方法**: PUT
- **路径**: `/api/strategies/{id}`
- **描述**: 更新指定ID的策略研报

#### 路径参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| id | int | 是 | 策略ID |

#### 请求头
```
Content-Type: application/json
```

#### 请求参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| categoryId | int | 是 | 策略类别ID（1=期货，2=加密货币） |
| title | string | 是 | 研报标题 |
| htmlContent | string | 是 | HTML格式的研报内容 |
| summary | string | 否 | 摘要数据（JSON格式） |
| isEnabled | bool | 否 | 是否启用，默认true |
| remarks | string | 否 | 备注信息 |

#### 请求示例
```bash
PUT /api/strategies/123
Content-Type: application/json

{
  "categoryId": 1,
  "title": "2024年期货市场研报（更新版）",
  "htmlContent": "<h1>期货市场深度分析</h1><p>更新的市场分析内容...</p>",
  "summary": "{\"keyPoints\":[\"深度分析\",\"新趋势\"],\"riskLevel\":\"高\"}",
  "isEnabled": true,
  "remarks": "2024年第二季度更新"
}
```

#### 响应格式
```json
{
  "success": true,
  "message": "策略更新成功"
}
```

### 4. 获取研报详情

#### 接口信息
- **方法**: GET
- **路径**: `/api/strategies/{id}`
- **描述**: 获取指定策略的详细信息

#### 路径参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| id | int | 是 | 策略ID |

#### 请求示例
```bash
GET /api/strategies/123
```

#### 响应格式
```json
{
  "success": true,
  "data": {
    "id": 123,
    "category": "Futures",
    "categoryId": 1,
    "title": "2024年期货市场研报",
    "htmlContent": "<h1>期货市场分析</h1><p>详细的市场分析内容...</p>",
    "summary": "{\"keyPoints\":[\"技术指标\",\"市场趋势\"],\"riskLevel\":\"中\"}",
    "isEnabled": true,
    "createdAt": "2024-01-15T10:30:00",
    "updatedAt": "2024-01-15T14:20:00",
    "remarks": "期货市场分析"
  }
}
```

### 5. 获取研报HTML预览

#### 接口信息
- **方法**: GET
- **路径**: `/api/strategies/{id}/html`
- **描述**: 获取策略的HTML内容，用于网页预览

#### 路径参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| id | int | 是 | 策略ID |

#### 请求示例
```bash
GET /api/strategies/123/html
```

#### 响应格式
直接返回HTML内容，Content-Type: text/html

### 6. 删除研报

#### 接口信息
- **方法**: DELETE
- **路径**: `/api/strategies/{id}`
- **描述**: 删除指定的策略研报

#### 路径参数
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| id | int | 是 | 策略ID |

#### 请求示例
```bash
DELETE /api/strategies/123
```

#### 响应格式
```json
{
  "success": true,
  "message": "策略删除成功"
}
```

## 📝 数据格式说明

### HTML内容格式
研报内容以HTML格式存储，支持：
- 标准的HTML标签
- 内联样式
- 图片链接（支持外部URL）
- 表格、列表等复杂布局
- 富文本格式

### 摘要数据格式
摘要数据以JSON格式存储，可以包含以下类型的结构化信息：

**基础结构示例：**
```json
{
  "keyPoints": [
    "技术分析指标",
    "市场趋势预测",
    "风险评估要点"
  ],
  "riskLevel": "中",
  "confidence": 75,
  "timeframe": "1-3个月"
}
```

**期货策略摘要示例：**
```json
{
  "keyPoints": [
    "MACD指标金叉",
    "布林带收缩",
    "成交量放大"
  ],
  "riskLevel": "中",
  "targetPrice": {
    "entry": 3650,
    "stopLoss": 3550,
    "takeProfit": 3850
  },
  "direction": "多头",
  "leverage": "3倍",
  "confidence": 80,
  "timeframe": "1-2周",
  "indicators": {
    "macd": "金叉",
    "rsi": 65,
    "bollinger": "收缩中"
  }
}
```

**加密货币策略摘要示例：**
```json
{
  "keyPoints": [
    "比特币突破历史高点",
    "以太坊网络升级完成",
    "机构投资者持续流入"
  ],
  "riskLevel": "高",
  "marketCap": "1.2万亿美元",
  "tradingVolume": "日均5000亿美元",
  "topCoins": [
    "BTC", "ETH", "BNB", "ADA", "SOL"
  ],
  "confidence": 70,
  "timeframe": "3-6个月",
  "sentiment": {
    "overall": "乐观",
    "fearGreedIndex": 75
  }
}
```

### HTML内容示例
```html
<h1>2024年期货市场研报</h1>

<h2>市场概况</h2>
<p>2024年期货市场整体呈现...</p>

<h2>技术分析</h2>
<ul>
  <li>支撑位：3500点</li>
  <li>阻力位：3800点</li>
</ul>

<h2>投资建议</h2>
<table border="1">
  <tr>
    <th>合约</th>
    <th>操作建议</th>
    <th>目标价位</th>
  </tr>
  <tr>
    <td>IF2406</td>
    <td>买入</td>
    <td>3700-3750</td>
  </tr>
</table>

<img src="https://example.com/chart.png" alt="技术图表" style="max-width: 100%;">
```

## 🔧 客户端集成示例

### JavaScript (Fetch API)
```javascript
// 上传新研报（包含摘要）
async function uploadReport(title, htmlContent, summaryJson, categoryId = 1) {
  const response = await fetch('/api/strategies', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      categoryId: categoryId,
      title: title,
      htmlContent: htmlContent,
      summary: summaryJson, // JSON字符串格式的摘要数据
      isEnabled: true,
      remarks: 'API上传'
    })
  });

  const result = await response.json();
  return result;
}

// 获取研报列表
async function getReports(category = null) {
  const url = category ? `/api/strategies/list?category=${category}` : '/api/strategies/list';
  const response = await fetch(url);
  const result = await response.json();
  return result;
}

// 获取研报摘要
async function getReportSummary(id) {
  const response = await fetch(`/api/strategies/${id}/summary`);
  if (response.ok) {
    return await response.json(); // 返回JSON对象
  } else {
    throw new Error('获取摘要失败');
  }
}

// 获取研报摘要
async function getReportSummary(id) {
  const response = await fetch(`/api/strategies/${id}/summary`);
  if (response.ok) {
    return await response.json(); // 返回JSON对象
  } else {
    throw new Error('获取摘要失败');
  }
}

// 上传研报（包含摘要）
async function uploadReportWithSummary(title, htmlContent, summaryJson, categoryId = 1) {
  const response = await fetch('/api/strategies', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      categoryId: categoryId,
      title: title,
      htmlContent: htmlContent,
      summary: summaryJson, // JSON字符串
      isEnabled: true,
      remarks: 'API上传'
    })
  });

  const result = await response.json();
  return result;
}
```

### C# (HttpClient)
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class StrategyApiClient
{
    private readonly HttpClient _httpClient;

    public StrategyApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // 上传研报（包含摘要）
    public async Task<bool> UploadReportWithSummaryAsync(string title, string htmlContent, string summaryJson, int categoryId = 1)
    {
        var request = new
        {
            categoryId = categoryId,
            title = title,
            htmlContent = htmlContent,
            summary = summaryJson, // JSON字符串格式的摘要数据
            isEnabled = true,
            remarks = "API上传"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/strategies", content);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

        return result?.success ?? false;
    }

    // 获取研报列表
    public async Task<List<StrategyInfo>> GetReportsAsync(int? category = null)
    {
        var url = "/api/strategies/list";
        if (category.HasValue)
        {
            url += $"?category={category}";
        }

        var response = await _httpClient.GetAsync(url);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<StrategyInfo>>>();

        return result?.data ?? new List<StrategyInfo>();
    }

    // 获取研报摘要
    public async Task<T> GetReportSummaryAsync<T>(int id)
    {
        var response = await _httpClient.GetAsync($"/api/strategies/{id}/summary");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        throw new HttpRequestException($"获取摘要失败: {response.StatusCode}");
    }

    // 获取研报摘要
    public async Task<T> GetReportSummaryAsync<T>(int id)
    {
        var response = await _httpClient.GetAsync($"/api/strategies/{id}/summary");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        throw new HttpRequestException($"获取摘要失败: {response.StatusCode}");
    }

    // 上传研报（包含摘要）
    public async Task<bool> UploadReportWithSummaryAsync(string title, string htmlContent, string summaryJson, int categoryId = 1)
    {
        var request = new
        {
            categoryId = categoryId,
            title = title,
            htmlContent = htmlContent,
            summary = summaryJson,
            isEnabled = true,
            remarks = "API上传"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/strategies", content);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

        return result?.success ?? false;
    }
}

public class ApiResponse
{
    public bool success { get; set; }
    public string? message { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? data { get; set; }
}

public class StrategyInfo
{
    public int id { get; set; }
    public string category { get; set; }
    public int categoryId { get; set; }
    public string title { get; set; }
    public bool isEnabled { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public string? remarks { get; set; }
}
```

## ⚠️ 错误处理

### 常见错误码
| HTTP状态码 | 错误信息 | 说明 |
|------------|----------|------|
| 400 | 标题不能为空 | title字段为空 |
| 400 | HTML内容不能为空 | htmlContent字段为空 |
| 400 | 无效的策略类别 | categoryId不在有效范围内 |
| 404 | 策略不存在 | 指定的策略ID不存在 |
| 500 | 服务器内部错误 | 服务端处理异常 |

### 错误响应格式
```json
{
  "success": false,
  "message": "错误描述",
  "error": "详细错误信息"
}
```

## 🛠️ 管理界面

系统提供Web管理界面，访问路径：`/api/docs/strategy-management`

管理界面功能：
- 📋 策略列表查看
- ➕ 新建策略
- ✏️ 编辑策略
- 👁️ HTML预览
- 📄 查看摘要（JSON格式）
- 🗑️ 删除策略
- 🔍 按类别筛选

### 摘要查看功能
在策略列表的操作列中，点击 **"摘要"** 按钮可以查看该策略的摘要信息：
- 以格式化的JSON格式显示摘要数据
- 支持复杂的数据结构展示
- 如果策略没有摘要信息，会显示相应的提示

## 📊 数据限制

- **标题长度**: 最大200字符
- **HTML内容**: 无长度限制，但建议控制在合理范围内
- **备注长度**: 最大500字符
- **类别**: 仅支持1（期货）和2（加密货币）

## 🔄 版本信息

- **API版本**: v1.0
- **最后更新**: 2024-01-15
- **兼容性**: 向后兼容

---

如有问题请联系技术支持团队。

---

## 🚀 数据库升级说明

### 服务器部署时自动升级

系统会在启动时自动检测并升级数据库结构：

1. **程序启动时**：DatabaseInitializationService 会检查 Strategies 表结构
2. **自动添加字段**：如果缺少 Summary 字段，会自动添加
3. **向后兼容**：升级不会影响现有数据

### 手动升级（如果需要）

如果遇到数据库问题，可以按照以下步骤手动升级：

1. **备份数据库**：复制 `data/register.db` 文件
2. **重启服务**：系统会自动检测并升级表结构
3. **验证升级**：检查服务器日志中的数据库升级信息

### 升级日志示例

```
[DATABASE] ✅ Strategies 表已存在
[DATABASE] 检测到 Strategies 表缺少 Summary 字段，正在升级...
[DATABASE] ✅ Strategies 表升级成功，添加了 Summary 字段
```

### 故障排除

如果升级失败：
1. 检查数据库文件权限
2. 确认 SQLite 版本兼容性
3. 查看详细错误日志
4. 联系技术支持恢复备份

---

## 🔄 版本信息

- **API版本**: v2.0 (包含摘要功能)
- **最后更新**: 2026-01-17
- **兼容性**: 向后兼容，但推荐使用新路径 `/api/strategies`

### 📋 变更日志

**v2.0 (2026-01-17)**
- ✅ 新增策略摘要功能，支持JSON格式摘要数据存储
- ✅ 新增 `GET /api/strategies/{id}/summary` 接口
- ✅ 更新所有API路径从 `/api/strategy` 到 `/api/strategies`
- ✅ 自动数据库升级，支持添加Summary字段
- ✅ 改进错误处理和用户体验

**v1.0 (初始版本)**
- ✅ 基础策略CRUD功能
- ✅ HTML内容存储和管理
- ✅ 分类筛选功能

