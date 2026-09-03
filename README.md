# dividend-harvest

## Runtime configuration

运行时配置位于 Host 项目目录：

- `src/DividendHarvest/appsettings.json`：本地开发默认配置。
- `src/DividendHarvest/appsettings.Production.json`：生产环境配置，SQLite 默认写入 `/app/data/dividend-harvest.db`。

ASP.NET Core 会在 `Production` 环境下自动加载 `appsettings.Production.json`；如使用环境变量部署，环境变量仍可覆盖文件中的值。

## FTShare MCP

首次建账通过后端 FTShare MCP Adapter 获取 A 股股票基础资料。请在生产配置文件中填写 MCP 地址，或通过环境变量覆盖；不需要在代码、镜像或 Git 中保存 FTShare key：

```text
FtShare__McpEndpoint=https://<ftshare-mcp-endpoint>/mcp
FtShare__StockProfileToolName=get_stock_profile
FtShare__StockMarketDataToolName=get_stock_market_data
FtShare__StockDividendEventsToolName=get_stock_dividend_events
FtShare__StockFinancialSnapshotsToolName=get_stock_financial_snapshots
FtShare__SecurityCodeArgumentName=security_code
FtShare__ExchangeCodeArgumentName=exchange_code
FtShare__RequestTimeoutSeconds=30
FtShare__MaxRetryCount=2
FtShare__RetryDelayMilliseconds=250
```

Adapter 使用 Streamable HTTP；股票代码和交易所参数会按项目的 `security_code`、`exchange_code` canonical 字段发送，返回资料会规范化为 A 股、CNY 数据。
