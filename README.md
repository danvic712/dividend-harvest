# dividend-harvest

## FTShare MCP

首次建账通过后端 FTShare MCP Adapter 获取 A 股股票基础资料。部署时只需注入 MCP 地址和工具配置，不需要在代码、镜像或 Git 中保存 FTShare key：

```text
FtShare__McpEndpoint=https://<ftshare-mcp-endpoint>/mcp
FtShare__StockProfileToolName=get_stock_profile
FtShare__SecurityCodeArgumentName=security_code
FtShare__ExchangeCodeArgumentName=exchange_code
FtShare__RequestTimeoutSeconds=30
```

Adapter 使用 Streamable HTTP；股票代码和交易所参数会按项目的 `security_code`、`exchange_code` canonical 字段发送，返回资料会规范化为 A 股、CNY 数据。
