# Dividend Harvest 后端架构设计

## 1. 目标与边界

Dividend Harvest 是一个面向个人 A 股长期投资者的私人工具。后端使用 ASP.NET Core Controllers，前端和后端最终由同一个 Host 镜像提供。后端负责持久化用户的组合、持仓、交易和模型数据，并通过 FTShare MCP Adapter 获取外部股票资料、行情、股息和财务数据。

系统只提供可解释的参考结果，不执行交易，也不把外部数据源当作用户持仓或交易记录的归属地。

## 2. 分层与依赖方向

```text
DividendHarvest Host
├── Application.Contracts / Dtos / Exceptions / Validators / Mapping
├── Application.Setup
├── Application.Stocks
├── Application.Portfolio
├── Application.DividendStrategy
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
│   │   ├── Security.cs                    # 可选 sector_code
│   │   ├── ModelParameterSet.cs
│   │   ├── PriceObservation.cs
│   │   ├── DividendEvent.cs
│   │   ├── FinancialSnapshot.cs
│   │   ├── CashLedgerEntry.cs
│   │   ├── RecommendationSnapshot.cs
│   │   └── PortfolioTrade.cs
│   ├── Enums/                          # 枚举；当前暂无枚举
│   ├── Portfolio/                      # 持仓领域规则
│   ├── Securities/                     # A 股标识领域规则
│   └── DividendModel/                  # 股息率与价格区域计算
├── DividendHarvest.Application/
│   ├── Contracts/                      # Application 对外 Interface
│   │   ├── ISetupAppService.cs
│   │   ├── IStockDataProvider.cs
│   │   ├── IStockWatchlistAppService.cs
│   │   ├── IStockModelParameterAppService.cs
│   │   ├── IStockPriceObservationAppService.cs
│   │   ├── IStockDividendEventAppService.cs
│   │   ├── IStockAnalysisAppService.cs
│   │   ├── IStockFinancialSnapshotAppService.cs
│   │   ├── IBudgetAppService.cs
│   │   ├── IPortfolioRecommendationAppService.cs
│   │   ├── IRecommendationSnapshotAppService.cs
│   │   └── IStockDailyDataSyncAppService.cs
│   ├── Dtos/                           # 所有 Application DTO
│   ├── Exceptions/                     # 所有 Application 自定义 Exception
│   ├── Validators/                     # FluentValidation 请求验证器
│   ├── Mapping/                        # Mapperly 编译期对象映射
│   │   └── ApplicationMapper.cs
│   ├── ApplicationServiceCollectionExtensions.cs
│   ├── Setup/                          # AppService 实现和用例编排
│   ├── Stocks/                         # 股票资料、关注列表和交易日数据同步
│   │   ├── StockWatchlistAppService.cs
│   │   ├── StockPriceObservationAppService.cs
│   │   ├── StockDividendEventAppService.cs
│   │   ├── StockFinancialSnapshotAppService.cs
│   │   ├── StockDailyDataSyncAppService.cs
│   │   └── DailySyncSchedule.cs
│   ├── Portfolio/                      # 组合现金流水、交易和持仓变更
│   │   ├── BudgetAppService.cs
│   │   └── PortfolioTradeAppService.cs
│   └── DividendStrategy/               # 模型参数、单股分析和组合建议
│       ├── StockModelParameterAppService.cs
│       ├── StockAnalysisAppService.cs
│       ├── PortfolioRecommendationAppService.cs
│       └── RecommendationSnapshotAppService.cs
├── DividendHarvest.Infrastructure/
│   ├── Contracts/                      # Infrastructure Adapter Interface
│   │   └── IFtShareMcpToolInvoker.cs
│   ├── Configurations/                 # EF Core Fluent Entity Configuration
│   ├── Repositories/                   # Repository、Uow 实现
│   │   ├── EFRepository.cs
│   │   └── EFUow.cs
│   ├── InfrastructureServiceCollectionExtensions.cs
│   ├── DividendHarvestDbContext.cs     # EF Core DbContext
│   └── FtShare/                        # FTShare MCP Adapter 实现
└── DividendHarvest/                    # ASP.NET Core Controllers Host
    ├── appsettings.json                # 本地默认配置
    ├── appsettings.Production.json     # 生产环境配置
    ├── Controllers/                    # 业务 HTTP Controller
    │   ├── SetupController.cs
    │   ├── StocksController.cs
    │   ├── BudgetsController.cs
    │   ├── RecommendationsController.cs
    │   └── PortfolioController.cs
    ├── Background/                     # ASP.NET Core 后台调度
    │   └── DailyStockDataSyncHostedService.cs
    ├── HostServiceCollectionExtensions.cs
    ├── WebApplicationExtensions.cs
    └── HealthChecks/                   # 原生 ASP.NET Core Health Checks
```

Application 的业务实现按业务能力归并到 `Setup`、`Stocks`、`Portfolio` 和 `DividendStrategy` 四个目录。目录归并不等于合并 Interface：价格、股息和财务同步仍然保持独立的 Interface 与 AppService，因为它们具有不同的数据校验、幂等键和异常语义；单股分析与组合建议也保持独立。`Contracts`、`Dtos`、`Exceptions` 和 `Validators` 继续作为 Application 根目录的规范容器，不在每个业务目录下重复创建，避免物理目录再次膨胀。

`Stocks` 同时承载交易日同步编排，因为该编排只围绕关注股票的外部事实更新；如果未来出现多个互不相关的调度任务，再单独引入 `Operations` 模块。`StockModelParameterAppService` 归入 `DividendStrategy`，因为模型参数是分析和组合建议的输入，而不是持仓或现金流水本身。

各层的依赖注入通过对应的扩展类集中注册：Application 使用 `ApplicationServiceCollectionExtensions.AddDividendHarvestApplication`，Infrastructure 使用 `InfrastructureServiceCollectionExtensions.AddDividendHarvestInfrastructure`，Host 使用 `HostServiceCollectionExtensions.AddDividendHarvestHost`。Host 对 `WebApplication` 的异常处理中间件、Controller/健康检查路由和数据库初始化统一放在 `WebApplicationExtensions`；`Program.cs` 只保留配置构建、扩展调用、应用构建和启动顺序。

公共基础能力也遵循相同的组合边界：Swagger/Serilog 注册在 `HostServiceCollectionExtensions`，Swagger UI、Serilog HTTP 请求日志中间件和其他 `WebApplication` 行为在 `WebApplicationExtensions`；Application 的 Mapperly 映射定义集中在 `Mapping/ApplicationMapper.cs`，由构建期生成实际映射代码。

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

### 5.3 公共对象映射

- Riok.Mapperly 是本项目的默认对象映射框架。它在编译期生成映射代码，减少运行时反射和隐式约定，适合当前的单镜像部署。
- 映射声明放在 `Application/Mapping/`，使用 partial mapper 和显式的 MapProperty/额外映射参数表达名称差异。
- Application 返回 DTO，不返回 Domain Model；AppService 负责领域编排，Mapper 只负责数据形状转换，不包含查询、验证、事务或业务规则。
- 需要上下文才能完成的输出映射可以使用额外映射参数；仍然无法由通用 Mapper 表达的派生计算结果，应保留在 AppService/Domain，不要强行塞入映射器。
- 新增 DTO 或 Model 字段时，必须检查 Mapperly 的编译期诊断并补充对应映射；不得通过关闭警告掩盖未映射字段。

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
      ├── Get<PortfolioPosition>()      │
      └── Get<ModelParameterSet>()      │
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
- Host 的 `/healthz`、`/readyz` 使用 ASP.NET Core 原生 Health Checks；数据库健康检查通过 `IUow.CanConnectAsync` 实现。健康检查是运行状态入口，不属于业务 API 版本范围。
- Host 的启动建库只能通过 `IUow.EnsureCreatedAsync`，不直接解析 DbContext。
- 数据库通过 Docker volume 持久化到 `/app/data`；镜像本身不保存用户数据。

## 7. Domain Models 与 Fluent 配置

数据库实体位于 `Domain/Models/`，只保存实体状态，不依赖 EF Core。所有表名、列名、长度、精度、索引、主键和外键关系均位于 Infrastructure 的独立 Fluent Configuration 文件中：

- `Configurations/PortfolioConfiguration.cs`
- `Configurations/PortfolioPositionConfiguration.cs`
- `Configurations/SecurityConfiguration.cs`
- `Configurations/ModelParameterSetConfiguration.cs`
- `Configurations/PriceObservationConfiguration.cs`
- `Configurations/DividendEventConfiguration.cs`
- `Configurations/FinancialSnapshotConfiguration.cs`
- `Configurations/CashLedgerEntryConfiguration.cs`
- `Configurations/RecommendationSnapshotConfiguration.cs`
- `Configurations/PortfolioTradeConfiguration.cs`

`DividendHarvestDbContext.OnModelCreating` 使用：

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(DividendHarvestDbContext).Assembly);
```

因此实体类不使用 Data Annotation，也不需要知道数据库表结构。新增实体时必须同时新增对应的 `IEntityTypeConfiguration<TEntity>` 文件；一个配置文件只负责一个实体。

当前基础关系：

```text
Portfolio 1 ───────────── * PortfolioPosition * ───────────── 1 Security
Portfolio 1 ───────────── * ModelParameterSet * ───────────── 1 Security
Security 1 ───────────── * PriceObservation
Security 1 ───────────── * DividendEvent
Security 1 ───────────── * FinancialSnapshot
Portfolio 1 ──────────── * CashLedgerEntry
Security 0..1 ────────── * CashLedgerEntry
Portfolio 1 ──────────── * RecommendationSnapshot
Security 1 ───────────── * RecommendationSnapshot
ModelParameterSet 0..1 ─ * RecommendationSnapshot
Portfolio 1 ──────────── * PortfolioTrade
Security 1 ───────────── * PortfolioTrade
```

组合删除持仓采用级联关系；股票删除持仓采用限制关系；股票的 `ExchangeCode + SecurityCode` 具有唯一索引。

## 8. Application 用例数据流

首次建账的调用路径如下：

```text
Controller POST /api/v1/setup
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

### 8.1 Controller 与请求验证

Controller 只负责 HTTP 绑定、调用 `I*AppService` 和返回响应，不包含业务计算、数据库访问、外部调用或手写输入判断。来自 HTTP、MCP 和定时任务的请求参数在 Application 边界通过 FluentValidation 验证；验证器位于 `Application/Validators/`，并通过 Application 程序集扫描注册。

FluentValidation 负责请求形状、字段范围、跨字段关系和集合重复项；Domain 仍负责不可绕过的业务不变量。验证失败转换为统一的 400 ProblemDetails，已知 Application 异常由 Host 的统一异常处理器映射为稳定的 HTTP 响应，避免在每个 Controller 中复制异常分支。

### 8.2 Watchlist 查询

`GET /api/v1/stocks` 通过 `IStockWatchlistAppService` 返回已配置的 A 股股票，按股票代码和交易所稳定排序；如果存在对应的 `PortfolioPosition`，响应同时包含当前持股、核心仓、目标股数和平均成本，否则 `holding` 为 `null`。Controller 不直接查询 `Security` 或 `PortfolioPosition`，股票资料和持仓摘要由 Application 用例统一组装。

### 8.3 股票模型参数

`POST /api/v1/stocks/model-parameters` 为已配置股票保存一个不可覆盖的模型参数版本；参数按 `portfolio_id + security_id + effective_from_date` 唯一。`GET /api/v1/stocks/{securityCode}/{exchangeCode}/model-parameters` 返回当前日期已经生效的最新版本，未来版本不会提前使用。四个收益率阈值必须满足 `strong_buy > accumulation > partial_trim > aggressive_trim > 0`，预算、持仓上限、现金保留比例和交易费用参数均由 FluentValidation 与 Domain 规则共同约束。

Application 只返回 `StockModelParameterSet` DTO，不返回 `ModelParameterSet` 实体；重复生效日期返回 409，未配置股票返回 404，未完成首次建账返回 409。

### 8.4 收盘行情快照

`POST /api/v1/stocks/{securityCode}/{exchangeCode}/price-observations/sync` 通过 `IStockDataProvider.GetMarketDataAsync` 获取 FTShare 规范化的收盘行情，并按 `security_id + trading_date` 幂等保存到 `price_observations`。快照包含收盘价、交易日期、观测时间、来源记录和数据质量代码；缺少价格、日期或来源信息时不会提交数据库。

同一股票同一交易日重复同步时直接返回已有快照，不重复写入。外部行情不可用映射为 503，未配置股票映射为 404，请求参数仍由 FluentValidation 验证。

### 8.5 股息事实同步

`POST /api/v1/stocks/{securityCode}/{exchangeCode}/dividend-events/sync` 通过 `IStockDataProvider.GetDividendEventsAsync` 获取 FTShare 股息事实，并按 `security_id + source_record_id` 幂等保存到 `dividend_events`。事件保留股息类型、实施状态、公告/除息/发放日期、特别股息标记、来源和数据质量，拟派与已取消事件不会在同步阶段被改写为已实施。

批量同步先完成所有事件的身份和领域校验，再统一提交新增事件；空列表表示本次来源没有返回事件，不产生写入。后续 TTM 实际每股股息计算只选择已实施、常规现金且有有效除息日期的事件。

### 8.6 财务事实同步

`POST /api/v1/stocks/{securityCode}/{exchangeCode}/financial-snapshots/sync` 通过 `IStockDataProvider.GetFinancialSnapshotsAsync` 获取带数据日期的 FTShare 财务事实，并按 `security_id + data_as_of_date` 幂等保存到 `financial_snapshots`。快照包含盈利、股息支付率、三年平均股息支付率、P/B（PB）、ROE、来源和数据质量。

财务数据缺失或外部服务不可用不会产生部分提交；支付率原始值保留在事实表中，由 Domain 可靠性规则判断是否通过或失败。

### 8.7 组合现金流水与预算摘要

`POST /api/v1/budgets/entries` 通过 `IBudgetAppService` 记录组合层的实际现金流水，支持预算入金、已收到股息、买入、卖出、费用和现金调整。股票相关流水必须关联已配置的 A 股；代码和流水类型/方向由 FluentValidation 与 Domain 规则共同校验。`GET /api/v1/budgets/summary` 按组合汇总流入和流出，并计算 `max(total_inflow_amount - total_outflow_amount, 0)` 作为当前可用预算。

现金流水是用户实际资金的来源；预计股息不会自动写入流水，也不会因为 TTM 股息存在而增加预算。该摘要为股票分析和组合建议提供可追溯的预算输入；现金保留比例在建议计算中应用，再投资比例和自动记录实际股息仍由后续现金流水录入流程负责。

### 8.8 当前股票分析

`GET /api/v1/stocks/{securityCode}/{exchangeCode}/analysis` 通过 `IStockAnalysisAppService` 读取当前已生效模型参数、最近有效行情、股息事实和可选持仓，计算 TTM 实际每股股息、当前股息率及四个参考价格边界。价格区域按强买入、分批加仓、持有、减仓候选和激进减仓五档返回。

分析结果明确区分 `unavailable`、`cautious`、`failed`、`re_evaluate` 和价格区域；当前没有完整股息可靠性财务资料时只返回谨慎参考和 `no_action`，不生成买卖股数。可靠性通过后，Application 使用现金流水净余额、现金保留比例、组合中已有持仓市值、模型参数中的预算/仓位上限、目标股数、核心仓和交易单位调用 Domain `TradeQuantityCalculator`，返回建议买入/卖出股数和估算交易金额。多股票之间的资金排序和集中度竞争由组合建议用例统一处理，建议快照由独立用例保存。

### 8.9 多股票组合建议

`GET /api/v1/recommendations` 通过 `IPortfolioRecommendationAppService` 读取关注列表、每只股票的分析结果、有效模型参数和组合预算，在组合层统一分配本期可用资金。资金分配顺序固定为强买入区、分批加仓区，再按可靠性通过状态和目标股数缺口排序；相同条件保持关注列表的稳定顺序，不使用随机排序。

组合建议会先扣除组合现金保留比例，再逐只应用预算比例、单股/单次/单期金额上限、行业/单股/单期金额上限、交易单位和手续费，并把已分配的买入金额从剩余预算中扣除。减仓仍保护核心仓，不占用买入预算。`Security.SectorCode` 来自 FTShare 股票资料的可选 `sector_code`/`industry_code` 字段；缺失行业资料时跳过行业上限，不推断或伪造行业归属。

`POST /api/v1/recommendations/snapshots` 使用同一份组合建议生成唯一 `model_run_id`，在一个 UoW 提交中保存每只股票的 `RecommendationSnapshot`。快照保留原始数据日期、参数版本、状态、价格区域和建议数量，不覆盖行情、股息、财务或现金流水事实，便于复现和后续回放。

### 8.10 交易日数据同步

`IStockDailyDataSyncAppService` 按关注列表逐只编排行情、股息和财务快照同步；失败项记录股票、数据类型和可读原因，其他数据类型及其他股票继续执行，避免单个 FTShare 数据缺口阻断整批更新。`POST /api/v1/stocks/sync` 提供手动触发入口。

Host 的 `DailyStockDataSyncHostedService` 按 `DailySync:LocalTime` 和 `DailySync:TimeZoneId` 调度，默认使用上海时间每日 18:00，并跳过周末；A 股法定节假日由数据源实际返回结果决定，重复快照通过事实同步用例幂等处理。生产环境可以通过 `DailySync:Enabled=false` 关闭后台调度而保留手动接口。

### 8.11 交易记录与持仓成本

`POST /api/v1/portfolio/trades` 通过 `IPortfolioTradeAppService` 记录已发生的买入或卖出。买入会按成交价、数量和手续费重算加权平均成本；实际卖出只不能超过当前持股，允许真实历史交易使当前持仓低于核心仓。核心仓保护只用于系统生成普通减仓建议，不阻止用户补录已经发生的交易。提供 `source_record_id` 时，按组合范围幂等处理重复提交。每次新交易与对应买卖现金流水、手续费流水在同一个 UoW 中提交，实际现金余额不会依赖模型股息推算。

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

`IFtShareMcpToolInvoker` 是 Infrastructure 内部 seam，位于 `Infrastructure/Contracts/`。`FtShareStockDataProvider` 负责 FTShare 返回值到 `StockData`、`StockMarketData`、`StockDividendData` 和 `StockFinancialData` DTO 的规范化，只接受 A 股和 CNY 股票资料，并拒绝缺少关键字段或股票身份不匹配的结果。

MCP 地址、工具名、股票代码参数名、交易所参数名和请求超时通过运行时配置注入。FTShare key 不进入代码、DTO、日志、镜像前端资源或 Git。

## 10. 公共运行能力

### 10.1 Swagger 与 OpenAPI

Host 使用 Swashbuckle 生成 OpenAPI 文档并提供 Swagger UI。当前业务接口统一使用 URL path 版本 `/api/v1/...`，未带版本号的业务 URL 不会隐式映射到默认版本；`/swagger` 用于浏览和调用 Controller 接口，`/swagger/v1/swagger.json` 用于获取 v1 机器可读的 OpenAPI 文档。Swagger 按 `IApiVersionDescriptionProvider` 动态生成版本文档，未来增加 v2 时新增对应的 `[ApiVersion(2.0)]` Controller/Action 和 `v2` 文档分组，不修改既有 v1 合约。Swagger 的服务注册集中在 `HostServiceCollectionExtensions`，middleware 集中在 `WebApplicationExtensions`，不在 `Program.cs` 或 Controller 中重复配置。

### 10.2 Serilog

Host 使用 Serilog 接管 ASP.NET Core 和应用的 `ILogger<T>` 日志，配置来源为 `appsettings.json` 与 `appsettings.Production.json`，默认输出到容器标准输出。`UseSerilogRequestLogging` 记录 HTTP 请求摘要，业务日志使用结构化属性；请求体、Authorization、FTShare 凭据和原始外部响应不得写入日志。

### 10.3 Mapperly

Application 使用 Riok.Mapperly 生成编译期映射代码，统一的映射声明位于 `Application/Mapping/ApplicationMapper.cs`。Mapper 只负责 Domain Model、外部规范化 DTO 和响应 DTO 之间的数据形状转换；业务规则、数据查询、事务和派生建议仍由 Application/Domain 负责。

### 10.4 API Versioning

- 使用 `Asp.Versioning.Mvc` 和 `Asp.Versioning.Mvc.ApiExplorer` 为 Controller API 提供版本元数据与 OpenAPI 分组。
- 当前业务 API 为 v1，Controller 使用 `[ApiVersion(1.0)]`，路由使用 `api/v{version:apiVersion}/...`；调用方必须显式携带 `/api/v1/...`。
- Host 使用 `UrlSegmentApiVersionReader`，不假设缺失版本时自动使用默认版本，并通过 `ReportApiVersions` 返回支持/弃用版本信息。
- 健康检查、Swagger UI 和 Swagger JSON 使用自身的基础路径，不强行套用业务 API 版本。
- 新增破坏性合约时增加 v2 版本元数据和对应文档分组；v1 只在明确的弃用策略下变更，不能静默改变响应语义。

## 11. Exception 设计

Application 自定义异常统一位于 `Application/Exceptions/`：

- `SetupValidationException`：请求数据违反用例输入约束。
- `SetupAlreadyCompletedException`：系统已经完成首次建账。
- `StockDataUnavailableException`：用例无法获得可靠的股票基础资料。
- `StockDataProviderUnavailableException`：外部资料 Adapter 的连接、协议或配置失败，由 Setup 用例转换为 `StockDataUnavailableException`。

外部异常不会穿透到 HTTP 响应；Host 只把稳定的 Application 异常映射为 400、409 或 503。调用方主动取消的 `OperationCanceledException` 不转换，继续传播。

## 12. 测试策略

测试项目位于 `tests/`，当前只针对 Domain 和 Application 编写单元测试，使用 xUnit + Moq：

- Domain 测试领域不变量。
- Application 测试通过 `IRepository<TEntity>`、`IUow` 和数据提供 Adapter 的 Interface mock 验证用例行为。
- Application 测试不创建真实 DbContext，也不依赖 SQLite 或真实 FTShare 网络连接。
- Infrastructure 的 EF Fluent 配置和 Adapter 通过编译、依赖检查及后续专门测试验证；不把 EF Core 细节泄漏到 Application 单元测试。

所有测试必须保持对 Interface 的验证，而不是依赖具体实现内部结构。

## 13. 后续扩展

新增行情、股息、财务和建议计算功能时，优先遵守以下边界：

- 外部 FTShare 数据先进入 Infrastructure Adapter，再转换为 Application DTO。
- 事实数据和建议快照按 `docs/dividend-harvest-quant-model.md` 的 canonical 字段建模。
- 新增持久化能力先增加对应 Domain Model `class` 和 Fluent Configuration，再通过 `IUow.Get<TEntity>()` 获取通用 Repository；不要从 Host 或 AppService 直接使用 DbContext。
- 新增业务状态代码时使用显式枚举或 `*_code` 约定，并将枚举放入所属项目的 `Enums/` 文件夹。
