# Dividend Harvest 后端架构设计

## 1. 目标与边界

Dividend Harvest 是一个面向个人 A 股长期投资者的私人工具。后端使用 ASP.NET Core Controllers，前端和后端最终由同一个 Host 镜像提供。后端负责持久化用户的组合、持仓、交易和模型数据，并通过 FTShare MCP Adapter 获取外部股票资料、行情、股息和财务数据。

系统只提供可解释的参考结果，不执行交易，也不把外部数据源当作用户持仓或交易记录的归属地。

## 2. 分层与依赖方向

```text
DividendHarvest Host
├── Application.Contracts / Dtos / Exceptions
├── Application.Setup
└── Infrastructure
    ├── Contracts
    ├── Configurations
    ├── Repositories
    └── FtShare

Application  ──depends on──> Domain
Infrastructure ──depends on──> Application + Domain
Host ──composes──> Application + Infrastructure
Domain ──depends on──> nothing in this solution
```

Host 只负责 HTTP 路由、依赖注入组合和运行时启动；业务规则属于 Application 或 Domain；外部系统和 EF Core 实现属于 Infrastructure。

## 3. 运行时配置

Host 项目的配置文件位于 `src/DividendHarvest/`：

- `appsettings.json`：本地开发默认配置，使用工作目录下的 SQLite 文件。
- `appsettings.Production.json`：生产环境配置，使用 `/app/data/dividend-harvest.db`，适配 Docker volume 持久化。

ASP.NET Core 默认环境名为 `Production`（大小写不敏感）时，会自动加载 `appsettings.Production.json`。环境变量仍作为后置覆盖层，可用于部署时覆盖连接字符串、FTShare MCP 地址和工具参数。两个文件只保存非敏感默认值，FTShare key 不进入源代码、镜像或 Git。

## 4. Repo 分层布局

```text
src/
├── DividendHarvest.Domain/
│   ├── Contracts/                      # Repository、Uow 等持久化抽象
│   │   ├── IRepository.cs
│   │   └── IUow.cs
│   ├── Models/                         # 数据库实体模型
│   │   ├── Portfolio.cs
│   │   ├── PortfolioPosition.cs
│   │   └── Security.cs
│   ├── Enums/                          # 枚举；当前暂无枚举
│   ├── Portfolio/                      # 持仓领域规则
│   └── Securities/                     # A 股标识领域规则
├── DividendHarvest.Application/
│   ├── Contracts/                      # Application 对外 Interface
│   │   ├── ISetupAppService.cs
│   │   └── IStockDataProvider.cs
│   ├── Dtos/                           # 所有 Application DTO
│   ├── Exceptions/                     # 所有 Application 自定义 Exception
│   └── Setup/                          # AppService 实现和用例编排
├── DividendHarvest.Infrastructure/
│   ├── Contracts/                      # Infrastructure Adapter Interface
│   │   └── IFtShareMcpToolInvoker.cs
│   ├── Configurations/                 # EF Core Fluent Entity Configuration
│   ├── Repositories/                   # Repository、Uow 实现
│   │   ├── EFRepository.cs
│   │   └── EFUow.cs
│   ├── DividendHarvestDbContext.cs     # EF Core DbContext
│   └── FtShare/                        # FTShare MCP Adapter 实现
└── DividendHarvest/                    # ASP.NET Core Controllers Host
    ├── appsettings.json                # 本地默认配置
    ├── appsettings.Production.json     # 生产环境配置
    ├── Controllers/                    # 业务 HTTP Controller
    └── HealthChecks/                   # 原生 ASP.NET Core Health Checks
```

约束：

1. 本项目声明的所有 Interface 必须位于所属项目的 `Contracts/` 文件夹；一个文件只放一个 Interface。
2. 所有 DTO 必须位于 `Dtos/` 文件夹；一个 DTO 文件只允许包含一个 DTO class/record。
3. 所有自定义 Exception 必须位于 `Exceptions/` 文件夹；一个文件只放一个 Exception。
4. 如果新增枚举，必须放到所属项目的 `Enums/` 文件夹；当前代码没有枚举，不创建空的伪实现。
5. `Application` 的业务实现类使用 `*AppService` 后缀；对应 Interface 使用 `I*AppService`。
6. 数据访问实现按照 `salary-insights` 拆分到 `Infrastructure/Repositories/` 和 Infrastructure 根目录的 DbContext，不使用单独的 `Persistence` 命名层。

## 5. Interface 与 DTO 规则

### 5.1 Contracts 归属

- `Domain/Contracts`：跨层需要依赖的通用持久化抽象，包括 `IRepository<TEntity>` 和 `IUow`。
- `Application/Contracts`：用例和外部资料能力的抽象，包括 `ISetupAppService` 和 `IStockDataProvider`。
- `Infrastructure/Contracts`：Infrastructure 内部 Adapter 的可替换抽象，包括 `IFtShareMcpToolInvoker`。

Application 不认识 EF Core、SQLite、HTTP 或 MCP SDK。Host 只依赖 Application 的 AppService Interface 和 Infrastructure 的组合注册扩展，不直接使用数据访问实现。

### 5.2 DTO

Application 的 DTO 位于 `Application/Dtos/`，用于 Controller 与用例之间的输入输出，以及外部资料 Adapter 规范化后的结果。DTO 不承担数据库实体职责，也不包含 Repository、DbContext 或 MCP 客户端。

一个 DTO 文件只能包含一个 DTO 类型，文件名必须与类型名一致。

## 6. Uow 与 Repository 设计

本项目参考 `salary-insights` 的通用 EF Core 数据访问模式，但保留本项目的 `IUow` 命名和“所有数据访问只能通过 Uow”的约束。

### 5.1 Domain Contracts

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetQueryable(
        bool asNoTracking = false,
        params Expression<Func<TEntity, object>>[] includes);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);

    Task<int> RemoveWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}

public interface IUow
{
    IRepository<TEntity> Get<TEntity>() where TEntity : class;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
```

`IRepository<TEntity>` 提供实体集合的最小查询和写入能力；`IUow.Get<TEntity>()` 是 Application 获取 Repository 的唯一入口。Application 不注入具体 Repository，不保留按用例命名的专用 Repository Interface。

### 5.2 Infrastructure 实现

```text
SetupAppService
      │
      ▼
     IUow
      │
      ├── Get<Portfolio>() ─────────────┐
      ├── Get<Security>()               │
      └── Get<PortfolioPosition>()      │
                                       ▼
                              EFUow / EFRepository<TEntity>
                                       │
                                       ▼
                             DividendHarvestDbContext
                                       │
                                       ▼
                                SQLite / /app/data
```

- `EFUow` 使用按实体类型缓存的 lazy Repository，与 `salary-insights` 的 `ConcurrentDictionary<Type, Lazy<object>>` 模式一致。
- `EFRepository<TEntity>` 只包装 `DbContext.Set<TEntity>()`，负责查询、包含、添加和删除。
- `EFUow.CommitAsync` 统一调用 `SaveChangesAsync`；一个用例的多个实体写入在一次提交中完成。
- `DividendHarvestDbContext` 位于 Infrastructure 根目录；`EFRepository<TEntity>` 和 `EFUow` 位于 `Infrastructure/Repositories/`，均为 Infrastructure 内部实现，避免 Host/Application 绕过 `IUow` 直接访问 DbContext。
- Host 的 `/healthz`、`/readyz` 使用 ASP.NET Core 原生 Health Checks；数据库健康检查通过 `IUow.CanConnectAsync` 实现。
- Host 的启动建库只能通过 `IUow.EnsureCreatedAsync`，不直接解析 DbContext。
- 数据库通过 Docker volume 持久化到 `/app/data`；镜像本身不保存用户数据。

## 7. Domain Models 与 Fluent 配置

数据库实体位于 `Domain/Models/`，只保存实体状态，不依赖 EF Core。所有表名、列名、长度、精度、索引、主键和外键关系均位于 Infrastructure 的独立 Fluent Configuration 文件中：

- `Configurations/PortfolioConfiguration.cs`
- `Configurations/PortfolioPositionConfiguration.cs`
- `Configurations/SecurityConfiguration.cs`

`DividendHarvestDbContext.OnModelCreating` 使用：

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(DividendHarvestDbContext).Assembly);
```

因此实体类不使用 Data Annotation，也不需要知道数据库表结构。新增实体时必须同时新增对应的 `IEntityTypeConfiguration<TEntity>` 文件；一个配置文件只负责一个实体。

当前基础关系：

```text
Portfolio 1 ───────────── * PortfolioPosition * ───────────── 1 Security
```

组合删除持仓采用级联关系；股票删除持仓采用限制关系；股票的 `ExchangeCode + SecurityCode` 具有唯一索引。

## 8. Application 用例数据流

首次建账的调用路径如下：

```text
Controller POST /api/setup
        │
        ▼
ISetupAppService
        │
        ▼
SetupAppService
        ├── IUow.Get<Portfolio>() 检查是否已完成建账
        ├── IStockDataProvider 获取并校验每只 A 股资料
        ├── IUow.Get<TEntity>() 添加组合、股票和期初持仓
        └── IUow.CommitAsync() 一次提交
```

外部资料在进入实体前先被规范化为 Application DTO；DTO 通过校验后才转换为 Domain Model。任何 FTShare 失败都不会产生部分持久化写入。

## 9. FTShare MCP Adapter

```text
IStockDataProvider
        ▲
        │ satisfies
FtShareStockDataProvider
        │
        ▼
IFtShareMcpToolInvoker
        ▲
        │ satisfies
FtShareMcpToolInvoker ──> official MCP Client ──> FTShare MCP
```

`IFtShareMcpToolInvoker` 是 Infrastructure 内部 seam，位于 `Infrastructure/Contracts/`。`FtShareStockDataProvider` 负责 FTShare 返回值到 `StockData` DTO 的规范化，只接受 A 股和 CNY 资料，并拒绝缺少关键字段或股票身份不匹配的结果。

MCP 地址、工具名、股票代码参数名、交易所参数名和请求超时通过运行时配置注入。FTShare key 不进入代码、DTO、日志、镜像前端资源或 Git。

## 10. Exception 设计

Application 自定义异常统一位于 `Application/Exceptions/`：

- `SetupValidationException`：请求数据违反用例输入约束。
- `SetupAlreadyCompletedException`：系统已经完成首次建账。
- `StockDataUnavailableException`：用例无法获得可靠的股票基础资料。
- `StockDataProviderUnavailableException`：外部资料 Adapter 的连接、协议或配置失败，由 Setup 用例转换为 `StockDataUnavailableException`。

外部异常不会穿透到 HTTP 响应；Host 只把稳定的 Application 异常映射为 400、409 或 503。调用方主动取消的 `OperationCanceledException` 不转换，继续传播。

## 11. 测试策略

测试项目位于 `tests/`，当前只针对 Domain 和 Application 编写单元测试，使用 xUnit + Moq：

- Domain 测试领域不变量。
- Application 测试通过 `IRepository<TEntity>`、`IUow` 和数据提供 Adapter 的 Interface mock 验证用例行为。
- Application 测试不创建真实 DbContext，也不依赖 SQLite 或真实 FTShare 网络连接。
- Infrastructure 的 EF Fluent 配置和 Adapter 通过编译、依赖检查及后续专门测试验证；不把 EF Core 细节泄漏到 Application 单元测试。

所有测试必须保持对 Interface 的验证，而不是依赖具体实现内部结构。

## 12. 后续扩展

新增行情、股息、财务和建议计算功能时，优先遵守以下边界：

- 外部 FTShare 数据先进入 Infrastructure Adapter，再转换为 Application DTO。
- 事实数据和建议快照按 `docs/dividend-harvest-quant-model.md` 的 canonical 字段建模。
- 新增持久化能力先增加对应 Domain Model `class` 和 Fluent Configuration，再通过 `IUow.Get<TEntity>()` 获取通用 Repository；不要从 Host 或 AppService 直接使用 DbContext。
- 新增业务状态代码时使用显式枚举或 `*_code` 约定，并将枚举放入所属项目的 `Enums/` 文件夹。
