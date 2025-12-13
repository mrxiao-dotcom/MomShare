# MomShares 产品份额管理系统

## 项目概述

MomShares 是一个产品份额管理系统，用于管理产品、持有者及其份额关系。系统采用WPF桌面程序作为服务器控制端，提供Web API服务，支持管理员和持有者两种角色的Web访问。

## 项目结构

```
MomShares/
├── MomShares.Core/              # 核心类库（实体、枚举、接口）
├── MomShares.Infrastructure/    # 基础设施类库（EF Core、仓储、服务）
├── MomShares.Api/               # ASP.NET Core Web API
├── MomShares.Server/            # WPF控制端
└── docs/                        # 文档目录
```

## 技术栈

- **.NET 8.0**
- **ASP.NET Core Web API**
- **Entity Framework Core 8.0**
- **SQLite**
- **WPF** (服务器控制端)
- **React** (前端，待创建)

## 当前进度

### ✅ 已完成
- [x] 项目结构创建
- [x] 实体类设计（Product, Holder, ProductNetValue, HolderShare, ShareTransaction, Dividend, DividendDetail, CapitalIncrease, Admin, OperationLog）
- [x] 枚举类型（ShareTransactionType, OperatorType）
- [x] DbContext配置
- [x] 基础项目配置

### 🚧 进行中
- [ ] 依赖注入配置
- [ ] 认证授权模块
- [ ] API控制器
- [ ] WPF控制端
- [ ] React前端

## 快速开始

### 运行API服务

```bash
cd MomShares.Api
dotnet run
```

API服务将在 `https://localhost:5001` 或 `http://localhost:5000` 启动。

### 数据库

数据库文件 `MomShares.db` 将在首次运行时自动创建。

## 开发计划

详细开发计划请参考 `docs/开发方案文档.md`

## 文档

- [需求文档](docs/需求文档.md)
- [数据库设计文档](docs/数据库设计文档.md)
- [开发方案文档](docs/开发方案文档.md)

