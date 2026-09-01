# Dividend Harvest 股息收益与分批交易参考模型

> V1（Version 1，第一版）定位：面向个人 A 股（A-share）长期投资者的、规则透明的股息收益分析工具。
>
> 系统只提供可解释的操作建议，不预测股价、不自动下单、不保证收益。所有结果都必须显示数据日期、股息口径和使用的模型参数。

## 1. 设计结论

本模型不是“只买股息率最高的股票”，而是按以下顺序工作：

```text
数据是否可靠
    ↓
股息是否持续、是否可能承担得起
    ↓
质量和价格风险是否需要谨慎提示
    ↓
当前股息率处于哪个价格区域
    ↓
结合核心仓、卫星仓和预算给出建议股数
```

成熟的股息指数编制方法通常也采用类似的分层思路：

- 上证红利参考方案要求过去三年连续现金分红，并检查股利支付率，再按三年平均现金股息率选样。[上海证券交易所公告](https://www.sse.com.cn/market/sseindex/diclosure/c/c_20220327_5700336.shtml)
- S&P 中国 A 股高股息红利贵族方案要求至少五年股息稳定或增长，同时检查规模、流动性，并限制单只股票权重。[S&P China Dividend Aristocrats 方法](https://www.spglobal.com/spdji/tc/documents/methodologies/methodology-sp-china-div-arist-indices.pdf)
- MSCI 高股息方案同时检查股利支付率、股息持续性、盈利能力、盈利波动、负债和价格表现，而不是只按股息率排序。[MSCI High Dividend Yield 方法](https://www.msci.com/indexes/documents/methodology/3_MSCI_High_Dividend_Yield_Indexes_Methodology_20250829.pdf)
- FTSE 高股息方案使用未来十二个月股息率，并排除预计不分红或只依靠特别分红的股票，同时设置流动性和成分缓冲规则。[FTSE All-World High Dividend Yield 方法](https://www.lseg.com/en/ftse-russell/indices/ftse-all-world-high-dividend-yield-index)

这些方案不能直接复制成个人买卖规则，但可以作为本项目的设计原则：先排除明显不可靠的高股息，再讨论价格便不便宜。

### 1.1 V1 最小实现边界

为了让方案可以直接实现，V1 只保留四个主模块：

1. 使用最近十二个月已经实施的常规现金股息计算 TTM DPS；
2. 用三年连续分红、五年股息趋势和支付率做硬性可靠性检查；
3. 使用每只股票手工配置的四个股息率阈值计算五个价格区域；
4. 在预算、组合上限、核心仓和交易单位内计算建议股数。

以下内容在 V1 中只做展示或提醒，不直接产生买卖信号：ROE、盈利稳定性、负债覆盖、P/B（PB）、Forward DPS、历史股息率分位数和长期情景预测。过去一年明显下跌只会把状态降为 `cautious`，从而禁止自动买入。

最终建议按以下顺序产生：

```text
数据有效性 → 股息可靠性 → 价格区域 → 组合限制 → 核心/卫星仓 → 建议股数
```

`model_status_code = unavailable`、`failed` 或 `re_evaluate` 时，不得因为股息率很高而生成买入建议；`cautious` 只允许观察和持有，不自动买入。后面的规则不能覆盖前面的硬性阻断。

状态映射固定为：可靠性检查全部通过且没有风险提醒时为 `available`；数据不足或质量、下跌风险需要人工确认时为 `cautious`；支付率失控或股息趋势明确恶化时为 `failed`；已确认取消分红或重大基本面事件时为 `re_evaluate`。

计算器可以直接按以下伪代码实现：

```text
if price_or_model_data_invalid:
    status = unavailable; recommendation = no_action
elif confirmed_cancellation_or_major_event:
    status = re_evaluate; recommendation = re_evaluate
elif reliability_failed:
    status = failed; recommendation = no_action
elif reliability_cautious_or_risk_warning:
    status = cautious; recommendation = hold or no_action
else:
    status = available
    recommendation = recommendation_from_price_zone_and_position_limits
```

### 1.2 术语与关键缩写

以下名称按投资研究和指数编制中常见的英文术语统一。用户界面优先显示中文，首次出现时同时显示英文和缩写；接口、数据表和公式使用表中的规范名称。

| 中文名称 | 标准英文名称 | 缩写或公式写法 | 数据库字段名 |
| --- | --- | --- | --- |
| A 股 | A-share | `A-share` | `market_code` |
| 股息率 | Dividend Yield | `Dividend Yield` | `dividend_yield` |
| 模型每股股息 | Model Dividend Per Share | `D_model` | `model_dividend_per_share` |
| TTM 实际每股股息 | Trailing Twelve-Month Dividend Per Share | `TTM DPS` / `D_TTM` | `ttm_dividend_per_share` |
| 预计未来每股股息 | Forward Dividend Per Share | `Forward DPS` / `D_forward` | `forward_dividend_per_share` |
| 每股股息 | Dividend Per Share | `DPS` | `dividend_per_share` |
| 每股收益 | Earnings Per Share | `EPS` | `earnings_per_share` |
| 股利支付率 | Dividend Payout Ratio | `DPS ÷ EPS` | `dividend_payout_ratio` |
| 市净率 | Price-to-Book Ratio | `P/B`（中文市场常写 `PB`） | `price_to_book_ratio` |
| 净资产收益率 | Return on Equity | `ROE` | `return_on_equity` |
| 80 分位数 | 80th Percentile | `Q80` | 不作为 V1 持久化字段 |
| 核心仓 | Core Position | — | `core_shares` |
| 卫星仓 | Satellite Position | — | `satellite_shares` |
| 版本 1 | Version 1 | `V1` | `model_version` |

规范写法约定：正文使用 `P/B（PB）`，不把 `PB` 展开成其他含义；“预计股息”统一称为“预计未来每股股息（Forward DPS）”；“TTM 股息”统一称为“TTM 实际每股股息（TTM DPS）”。

`D_model`、`D_TTM` 和 `D_forward` 是本文的公式变量，不是数据库字段名。数据库中不使用 `dps`、`eps`、`pb`、`roe` 这类缩写字段；展示层可以继续显示行业通用缩写。

### 1.3 数据库命名与类型约定

SQLite 和 PostgreSQL 共用一套数据库命名规则：

1. 表名和字段名全部使用小写 `snake_case`，不使用驼峰、空格、中文或需要双引号包裹的大小写名称。
2. 字段名使用完整语义，不使用 `value`、`type`、`status`、`date`、`source` 等脱离上下文的通用名称；改用 `dividend_status_code`、`dividend_type_code`、`data_source`、`payment_date` 等具体名称。
3. `_date` 表示只有日期的日历值；`_at` 表示带时区的时间点；`_amount` 表示货币金额；`_ratio` 表示 0 到 1 的比例；`_shares` 或 `_quantity` 表示数量；`_code` 表示业务代码。
4. 股票代码使用 `TEXT`，不能使用整数，避免丢失 `000001` 等前导零。
5. PostgreSQL 的金额、价格和比例使用 `numeric(20,8)`；SQLite 使用 `NUMERIC` 亲和类型。应用层统一使用 Decimal 计算，并对写入精度、范围和读写往返做校验；V1 不依赖数据库直接完成金融计算。比例统一保存为小数，例如 5% 保存为 `0.05`。
6. PostgreSQL 的时间点使用 `timestamp with time zone` 并按 UTC 保存；SQLite 使用 UTC 的 ISO 8601 `TEXT`。两者都使用 `_at` 字段名，不使用含义不明确的 `timestamp` 字段名。
7. 布尔字段使用 `is_` 或 `has_` 开头，例如 `is_special_dividend`、`is_price_to_book_gate_enabled`。SQLite 中用 `0/1` 存储并加 `CHECK`，PostgreSQL 映射为 `boolean`。

PostgreSQL 未加引号的名称会折叠为小写，而 SQLite 的关键字集合也会随版本扩展；因此统一使用小写 `snake_case` 并主动避开英文关键字，可以减少迁移和 SQL 生成差异。[PostgreSQL 标识符规则](https://www.postgresql.org/docs/current/sql-syntax-lexical.html) · [SQLite 关键字](https://www.sqlite.org/lang_keywords.html) · [SQLite 类型系统](https://www.sqlite.org/datatype3.html)

### 1.4 量化领域的规范字段

以下字段名作为模型、数据同步和后续数据库表设计的 canonical names。表名可以按领域拆分，但字段名不要在不同表中重新发明同义词。

| 领域 | 推荐字段名 | 说明 |
| --- | --- | --- |
| 股票标识 | `security_code`、`exchange_code`、`security_name`、`market_code`、`currency_code` | `security_code` 为文本，例如 `000333` |
| 行情快照 | `trading_date`、`close_price`、`price_observed_at`、`data_source`、`source_record_id` | 不使用 `price`、`date` 这种脱离上下文的名称 |
| 股息记录 | `dividend_per_share`、`dividend_type_code`、`dividend_status_code`、`announcement_date`、`ex_dividend_date`、`payment_date`、`is_special_dividend` | 区分公告、除息和发放三个日期 |
| 数据时点 | `data_as_of_date`、`published_at`、`captured_at`、`data_quality_code` | 分别表示数据截至日期、公开时间、系统抓取时间和数据质量 |
| 财务指标 | `earnings_per_share`、`dividend_payout_ratio`、`three_year_average_dividend_payout_ratio`、`price_to_book_ratio`、`return_on_equity` | 三年平均支付率为派生指标；不使用 `eps`、`payout`、`pb`、`roe` |
| 模型参数 | `portfolio_id`、`security_code`、`strong_buy_yield_threshold`、`accumulation_yield_threshold`、`partial_trim_yield_threshold`、`aggressive_trim_yield_threshold`、`strong_buy_budget_ratio`、`accumulate_budget_ratio`、`partial_trim_ratio`、`aggressive_trim_ratio`、`max_security_weight`、`max_sector_weight`、`cash_reserve_ratio`、`max_single_trade_amount`、`max_period_budget_amount`、`transaction_fee_ratio`、`minimum_transaction_fee_amount`、`trading_lot_size`、`model_parameter_set_id` | 阈值和比例保存为小数；参数版本不可覆盖 |
| 模型股息输入 | `dividend_mode_code`、`ttm_dividend_per_share`、`forward_dividend_per_share`、`custom_dividend_per_share` | V1 默认使用 `ttm`；其他模式需用户主动开启 |
| 持仓 | `held_shares`、`core_shares`、`satellite_shares`、`target_shares`、`average_cost_per_share` | 不使用含义不清的 `position` 或 `cost` |
| 预算 | `period_budget_amount`、`carryover_cash_amount`、`received_dividend_amount`、`available_budget_amount`、`reinvestment_ratio`、`cash_direction_code` | 金额使用 `*_amount`，比例使用 `*_ratio` |
| 建议结果 | `model_status_code`、`dividend_reliability_code`、`price_zone_code`、`recommendation_code`、`suggested_buy_shares`、`suggested_sell_shares`、`suggested_trade_amount`、`computed_at`、`model_run_id`、`model_parameter_set_id` | 建议是快照结果，不覆盖原始数据 |

枚举字段使用明确的 `*_code` 名称，并在应用层维护代码表。例如：`dividend_status_code` 可使用 `implemented`、`proposed`、`cancelled`；`price_zone_code` 可使用 `strong_buy`、`accumulate`、`hold`、`partial_trim`、`aggressive_trim`。数据库不保存面向用户的中文展示文案。

V1 建议的代码值：

```text
dividend_status_code: implemented, proposed, cancelled
dividend_type_code: regular_cash, special_cash
dividend_mode_code: ttm, forward, custom
data_quality_code: valid, cautious, stale, missing, conflicted
cash_direction_code: inflow, outflow
entry_type_code: budget_deposit, dividend_received, buy, sell, fee, cash_adjustment
model_status_code: available, cautious, failed, unavailable, re_evaluate
dividend_reliability_code: passed, cautious, failed, unavailable
price_zone_code: strong_buy, accumulate, hold, partial_trim, aggressive_trim
recommendation_code: strong_buy, accumulate, hold, partial_trim, aggressive_trim, re_evaluate, no_action
```

### 1.5 V1 最小数据表

V1 不需要复杂的数据仓库。以下九张表足以支持行情、分红、持仓、预算和可复现建议：

| 表名 | 用途 | 最小字段 |
| --- | --- | --- |
| `portfolios` | 投资组合资料 | `portfolio_id`、`portfolio_name`、`currency_code` |
| `securities` | 股票基础资料 | `security_code`、`exchange_code`、`security_name`、`market_code`、`currency_code` |
| `price_observations` | 收盘行情 | `security_code`、`trading_date`、`close_price`、`price_observed_at`、`data_source`、`source_record_id` |
| `dividend_events` | 分红事实 | `security_code`、`dividend_per_share`、`dividend_type_code`、`dividend_status_code`、`announcement_date`、`ex_dividend_date`、`payment_date`、`is_special_dividend`、`published_at`、`captured_at`、`data_source`、`source_record_id`、`data_quality_code` |
| `financial_snapshots` | 财务事实和指标 | `security_code`、`data_as_of_date`、`published_at`、`captured_at`、`data_source`、`source_record_id`、`earnings_per_share`、`dividend_payout_ratio`、`price_to_book_ratio`、`return_on_equity`、`data_quality_code` |
| `portfolio_positions` | 当前持仓 | `portfolio_id`、`security_code`、`held_shares`、`core_shares`、`target_shares`、`average_cost_per_share` |
| `cash_ledger_entries` | 预算和实际现金流水 | `portfolio_id`、`entry_date`、`entry_type_code`、`cash_direction_code`、`cash_amount`、`security_code`、`source_record_id` |
| `model_parameter_sets` | 模型参数 | `model_parameter_set_id`、`portfolio_id`、`security_code`、`model_version`、四个收益率阈值、买卖比例、组合上限、交易费用、`trading_lot_size`、`effective_from_date` |
| `recommendation_snapshots` | 某次计算结果 | `model_run_id`、`portfolio_id`、`security_code`、`data_as_of_date`、`close_price`、`model_dividend_per_share`、`dividend_mode_code`、`model_status_code`、`dividend_reliability_code`、`price_zone_code`、`recommendation_code`、`dividend_yield`、`suggested_buy_shares`、`suggested_sell_shares`、`suggested_trade_amount`、`computed_at`、`model_parameter_set_id` |

事实表保存来源数据，建议快照保存可重算的派生结果。`dividend_yield`、`price_zone_code`、`suggested_buy_shares` 和 `suggested_sell_shares` 不应覆盖事实表中的数据。

V1 的参数按 `portfolio_id + security_code` 生效；如果未来需要组合级默认值，可以允许 `security_code` 为空，但同一次计算必须能解析出唯一的有效参数集。

V1 的最低约束：价格必须大于 0；股数和金额不能为负；现金流水通过 `cash_direction_code` 区分流入和流出；比例必须在 0 到 1 之间；核心仓不能大于当前持股；四个收益率阈值必须满足 `strong_buy > accumulation > partial_trim > aggressive_trim > 0`。`recommendation_snapshots` 使用 `model_run_id + portfolio_id + security_code` 作为唯一计算结果标识。股票代码在交易所内唯一；如果数据源允许不同交易所使用相同代码，子表外键使用 `exchange_code + security_code` 的组合。

## 2. 投资对象和仓位结构

系统支持多只关注股票。每只股票独立保存股票资料、行情、股息、持仓和模型参数；预算和风险限制还需要在组合层统一管理。

### 2.1 核心仓

核心仓是用户计划长期持有、减仓时必须优先保留的最低持股数量。

核心仓的作用是防止股票上涨后被全部卖出，也不是永远不能卖出的数量。如果公司出现股息取消、长期经营恶化或数据确认的重大风险，系统应发出“重新评估核心仓”提醒。

### 2.2 卫星仓

卫星仓是超出核心仓、可以用于阶段性波段操作的数量：

```text
卫星仓 = max(当前持股数量 - 核心仓数量, 0)
```

核心仓和卫星仓都按股数保存。当前持股低于核心仓时，卫星仓为 0，系统只能显示持有或重新评估提醒，不能产生普通减仓建议。

### 2.3 目标股数

目标股数只是长期积累规划参数，不是强制买入条件，也不是清仓条件。没有目标股数时，系统仍然可以计算股息率和操作区域，但不应显示“距离目标还差多少股”或“达到目标需要多久”。

## 3. 数据和股息口径

### 3.1 模型股息

模型股息是用于计算股息率和价格区域的每股年度现金股息。它不等同于用户已经收到的股息。

为了让 V1 可直接实现，实时建议默认只使用一种口径：

```text
TTM DPS = 最近 12 个日历月内、截至 data_as_of_date 已实施的常规现金股息之和
D_model = TTM DPS
```

分红事件按 `ex_dividend_date` 归入统计窗口；没有除息日的事件在 V1 不计入 TTM，并降低数据质量。分红金额必须使用拆分、合并等公司行动调整后的每股金额。

以下事件不计入默认模型股息：

1. `is_special_dividend = 1` 的特别股息；
2. `dividend_status_code = cancelled` 的已取消股息；
3. `dividend_status_code = proposed` 的拟派股息。

拟派股息可以展示，但不能算作用户已经收到的现金。实际到账股息只能来自现金流水，不能因为模型股息存在就增加可用预算。

### 3.1.1 可选股息模式

Forward DPS 和用户自定义股息保留为扩展字段，不是 V1 默认决策条件：

| 模式 | 规则 | 数据不足时 |
| --- | --- | --- |
| `ttm` | 使用 `ttm_dividend_per_share` | `unavailable` |
| `forward` | 仅使用有来源和发布日期的 `forward_dividend_per_share` | `unavailable`，不静默回退 |
| `custom` | 仅使用用户明确输入的 `custom_dividend_per_share` | `unavailable` |

用户明确开启 `forward` 或 `custom` 后，输出必须显示模式、来源、发布日期和不确定性。V1 不把 TTM 和 Forward DPS 用 `min()` 混合，避免不同期间、不同口径的数据被误算为同一个年度股息。

### 3.2 数据时点

每个影响信号的数据都必须带有：

```text
data_as_of_date  数据截至日期
published_at     数据公开或公告时间
data_source      数据来源
source_record_id 来源记录标识
data_quality_code 数据质量状态
```

历史回放时，只能使用当时已经公开的数据，不能使用后来修订的股息、盈利或 P/B（PB）数据。否则回测结果会提前知道未来信息。

### 3.3 模型不可用

出现以下情况时，模型状态为“模型不可用”，可以继续展示行情和持仓，但不能生成买入或普通减仓建议：

- 模型股息缺失、为 0 或为负数；
- 价格缺失，或不是截至 `data_as_of_date` 的最新有效收盘价；
- 股息事件状态冲突，无法确定哪些事件已实施；
- 模型参数不完整或阈值顺序错误。

如果支付率、盈利或其他可靠性数据缺失，但模型股息和价格仍可计算，模型状态为“谨慎参考”，而不是直接显示确定性建议。P/B（PB）或其他辅助指标缺失也采用同样处理。

## 4. 股息可靠性筛选

股息率高，只能说明“按当前价格计算的收益率高”，不能说明这笔股息未来一定存在。系统应先做简单的可靠性筛选。

### 4.1 V1 基础检查

按 `data_as_of_date` 计算最近完整年度的常规 DPS，并使用以下简单规则：

| 检查 | 通过条件 | 不通过结果 |
| --- | --- | --- |
| 三年连续分红 | 最近三个完整日历年每年至少一条 `implemented + regular_cash` 事件 | `failed` |
| 五年股息趋势 | 最近五个完整年度数据齐全，且最新年度 DPS 不低于最早年度 DPS | 数据不足为“谨慎参考”；明确净下降为 `failed` |
| 最新支付率 | `0 < dividend_payout_ratio < 1` | 缺失为“谨慎参考”；负数、0 或大于等于 1 为 `failed` |
| 三年平均支付率 | `0 < three_year_average_dividend_payout_ratio < 1` | 缺失为“谨慎参考”；不在范围内为 `failed` |
| 取消分红 | 最近一次已公布事件没有确认取消 | 确认取消为 `model_status_code = re_evaluate` |

只有所有检查通过时，`dividend_reliability_code = passed`。数据不足不能当作通过；“谨慎参考”只允许观察和持有，不允许自动买入。

### 4.2 财务质量检查

P/B（PB，Price-to-Book Ratio，市净率）不能代表完整的公司质量。V1 只把以下指标作为提示，不实现“三项中至少两项通过”的复杂质量评分：

- 盈利能力：例如 ROE（Return on Equity，净资产收益率）或其他适合行业的盈利指标；
- 盈利稳定性：例如近几年盈利波动或是否连续恶化；
- 偿付能力：例如负债水平、经营现金流对现金分红的覆盖。

如果这些指标明显恶化，系统显示“高股息可能来自股价下跌或基本面恶化”的提醒，并可将 `model_status_code` 降为 `cautious`。V1 不为金融、周期、公用事业等行业分别建立复杂阈值；行业化阈值属于后续版本。

### 4.3 下跌风险提醒

如果股价在过去一年出现明显下跌，系统应将模型状态降为“谨慎参考”，禁止自动买入，并显示：

```text
股息率变高可能主要来自股价下跌，需要先检查股息可持续性。
```

这不是自动卖出条件，也不改变核心仓规则，而是避免把“价格下跌造成的高股息率”误认为便宜。

## 5. 股息率和价格区域

### 5.1 基本公式

```text
当前股息率（Dividend Yield） = 模型每股股息 D_model ÷ 当前价格 P（Price）
参考价格 = 模型每股股息 D_model ÷ 目标股息率 Y（Target Dividend Yield）
```

目标股息率必须是大于 0 的小数。例如 5% 应保存为 0.05，而不是 5。

### 5.2 五个价格区域

每只股票使用四个目标股息率：

```text
Y_strong > Y_add > Y_reduce > Y_aggressive > 0
```

对应价格边界：

```text
P_strong     = D_model ÷ Y_strong
P_add        = D_model ÷ Y_add
P_reduce     = D_model ÷ Y_reduce
P_aggressive = D_model ÷ Y_aggressive
```

因为股息率越高，参考价格越低，所以价格边界应满足：

```text
P_strong < P_add < P_reduce < P_aggressive
```

价格区域定义如下：

| 当前价格 | 区域 | 解释 |
| --- | --- | --- |
| `P <= P_strong` | 强买入区 | 收益率明显高，可以使用较大一部分计划资金，但仍受单次上限约束 |
| `P_strong < P <= P_add` | 分批加仓区 | 适合按计划买入一小部分 |
| `P_add < P < P_reduce` | 持有区 | 继续持有，通常不主动买入或卖出 |
| `P_reduce <= P < P_aggressive` | 减仓候选区 | 价格偏高，可以考虑卖出一部分卫星仓 |
| `P >= P_aggressive` | 激进减仓区 | 价格明显偏高，可以卖出更多卫星仓，但普通减仓仍不能突破核心仓 |

### 5.3 阈值如何产生

不要把 5% 作为所有股票的统一标准。银行、公用事业、周期股和消费股的正常股息率区间不同。

V1 要求用户为每只股票配置四个阈值，并保存到 `model_parameter_sets`：

```text
Y_strong > Y_add > Y_reduce > Y_aggressive > 0
```

公式变量与数据库字段的对应关系为：

```text
Y_strong     = strong_buy_yield_threshold
Y_add        = accumulation_yield_threshold
Y_reduce     = partial_trim_yield_threshold
Y_aggressive = aggressive_trim_yield_threshold
```

缺少任何阈值或顺序错误时，模型状态为 `unavailable`，不强行生成价格阶梯。第一版不自动拟合历史分位数，避免参数漂移和未来数据泄漏。

Q80（80th Percentile，80 分位数）、Q60、Q40、Q20 可以在后续版本中作为参数建议，但必须先明确样本频率、最小样本量、复权方式和回测时点。它们不是成熟指数方法对本项目阈值的直接结论，也不作为 V1 持久化字段。

### 5.4 防止信号来回跳动

价格刚好在边界附近时，不能因为一个价格跳动就频繁改变状态。V1 采用最简单的规则：

- 每日收盘后计算信号；
- 新区域连续两个有效交易日成立后才更新建议；
- 不在 V1 同时引入进入缓冲区和退出缓冲区两套参数。

系统可以实时展示当前股息率，但操作建议应使用已确认的日级快照。

## 6. 买入建议股数

### 6.1 可用预算

组合或单只股票的本期可用预算为：

```text
B_period = 本期新增预算
         + 上期结转现金
         + 已实际收到的股息 × 计划再投资比例

available_budget_amount = max(
    B_period - 为满足 cash_reserve_ratio 所需保留的现金,
    0
)
```

`cash_reserve_ratio` 由组合层根据当前组合价值计算最低现金储备；`available_budget_amount` 已经扣除该储备。计划再投资比例只影响规划预算，不代表系统已经收到股息。未到账的预计股息不能直接作为现金使用。

### 6.2 区域资金比例

V1 将比例保存为模型参数，而不是写死在代码中。初始值如下：

| 区域 | 本次最多使用本期可用预算的比例 |
| --- | ---: |
| 强买入区 | 50% |
| 分批加仓区 | 25% |
| 持有区 | 0% |
| 减仓候选区 | 0% |
| 激进减仓区 | 0% |

对应字段为 `strong_buy_budget_ratio` 和 `accumulate_budget_ratio`，其余区域的买入比例为 0。50%、25% 等数值是可修改的起始参数，不是经过证明的最优比例。系统必须同时限制单只股票的最大仓位、单次最大金额和单期最大金额。

### 6.3 建议股数公式

组合额度先按收盘价估算：

```text
single_security_room = max(
    total_portfolio_value × max_security_weight - current_security_market_value,
    0
)

sector_room = max(
    total_portfolio_value × max_sector_weight - current_sector_market_value,
    0
)

target_share_gap = max(target_shares - held_shares, 0)
target_room = target_share_gap × 当前价格
```

```text
本次预算上限 = min(
  B_period × 当前区域资金比例,
  target_room（如果配置了目标股数，否则忽略），
  single_security_room,
  sector_room,
  单次交易金额上限
)

建议股数 = floor(
  本次预算上限 ÷ 当前价格
  ÷ 交易单位
) × 交易单位
```

V1 先按成交金额计算建议股数，再单独计算估算交易费用。如果加上费用后超过预算，则向下调整一个交易单位。费用模型只需要支持费率和最低费用两个参数，不在 V1 引入复杂税费预测。

如果建议股数不足一个交易单位，则建议股数为 0，剩余现金结转到下一期。

建议股数不能超过目标股数缺口；没有目标股数时，只受预算和风险上限约束。

## 7. 减仓建议股数

### 7.1 普通减仓

```text
最多可减仓股数 = max(当前持股 - 核心仓, 0)
```

只有 `model_status_code = available`、`dividend_reliability_code = passed`，且价格区域为 `partial_trim` 或 `aggressive_trim` 时，才生成普通减仓候选。其他状态只输出观察或重新评估提醒。

V1 起始建议：

| 区域 | 建议卖出卫星仓比例 |
| --- | ---: |
| 减仓候选区 | 25% |
| 激进减仓区 | 50% |

```text
建议减仓股数 = floor(
  最多可减仓股数 × 当前区域减仓比例
  ÷ 交易单位
) × 交易单位
```

建议结果必须同时显示：

- 当前核心仓；
- 当前卫星仓；
- 最多可减仓股数；
- 本次建议减仓股数；
- 减仓后核心仓和卫星仓。

### 7.2 估值减仓和基本面恶化要分开

价格进入高位区，只产生“普通减仓建议”。如果发生股息取消、连续削减、盈利恶化或偿付能力恶化，应产生单独的“重新评估”提醒。

核心仓保护不能演变为无条件长期持有。系统不自动卖出，但必须允许用户知道：核心仓保护规则和公司投资逻辑失效是两件不同的事。

## 8. P/B（PB）辅助规则

P/B（PB，Price-to-Book Ratio，市净率）保留为可选辅助条件，但不再作为主要质量判断：

```text
股息率区域通过 + P/B（PB）条件通过 = 正常建议
股息率区域通过 + P/B（PB）缺失    = 谨慎参考
股息率区域通过 + P/B（PB）条件不通过 = 观察或降低建议等级
```

P/B（PB）阈值应支持按行业或单只股票设置。P/B（PB）缺失时，如果用户开启了“硬性 P/B 门槛”，可以阻止买入建议；如果没有开启，只能降级为“谨慎参考”。P/B（PB）本身不能单独触发买入或卖出。

## 9. 多股票组合规则

多股票支持不仅是保存多只股票，还必须解决资金竞争和集中度问题。

组合层至少提供：

- 组合本期总预算；
- 单只股票最大组合占比；
- 单一行业最大组合占比；
- 现金保留比例；
- 单只股票和单次交易金额上限。

多只股票同时出现买入信号时，按以下顺序分配有限资金：

1. 强买入区优先于分批加仓区；
2. `dividend_reliability_code = passed` 优先于 `cautious`；
3. 距离单只股票目标仓位更远的优先；
4. 仍然相同则按用户指定顺序，不使用随机排序。

组合预算被用完后，其余股票显示“信号成立，但本期预算不足”，而不是继续给出无法执行的建议。

## 10. 长期积累规划

### 10.1 确定性计算

```text
目标缺口 = max(目标股数 - 当前持股, 0)
预计投入金额 = 目标缺口 × 计划价格 + 预计交易成本
预计年化模型股息 = 当前持股 × D_model
```

“预计年化模型股息”是按模型股息计算的税前参考值，不代表已经收到的现金。系统应同时保留基于交易记录的“实际收到股息”。

### 10.2 到达目标时间

达到目标的时间必须通过逐期模拟计算，不得简单使用“缺口金额 ÷ 月度预算”。模拟至少考虑：

- 每期投入金额；
- 当期价格情景；
- 模型股息情景；
- 计划再投资比例；
- 交易单位和交易成本；
- 剩余现金结转。

V1 使用三个情景：

| 情景 | 价格 | 模型股息 |
| --- | --- | --- |
| 保守 | 较高或不利价格 | 下降 |
| 基准 | 当前计划价格 | 保持不变 |
| 乐观 | 较低计划价格 | 温和增长 |

输出“预计范围”和关键假设，不输出看起来精确但没有依据的单一月份。

## 11. 系统输出

每只股票的操作建议至少包含：

```text
model_status_code
dividend_reliability_code
close_price
model_dividend_per_share
dividend_mode_code
dividend_yield
price_zone_code
recommendation_code
suggested_buy_shares
suggested_sell_shares
suggested_trade_amount
core_shares
satellite_shares
data_as_of_date
model_parameter_set_id
computed_at
```

用户界面再把代码转换为中文文案，并同时展示股息口径、数据日期、参数版本和建议原因。

示例：

```text
当前价格：8.17 元
模型股息：0.31 元/股
当前股息率：约 3.79%
股息模式：TTM 实际每股股息（TTM DPS）

当前状态：持有

原因：
当前收益率位于分批加仓区和减仓候选区之间。
股息数据为最近十二个月已实施的常规现金股息。
当前卫星仓不足以支持普通减仓，或核心仓需要优先保护。

建议：
继续持有。本期不主动买入，等待收益率进入更有吸引力的区域。
```

## 12. 回测和验收

在宣称策略有效之前，必须完成历史回放。回测规则至少包括：

1. 使用当时可获得的股息、财务和价格数据；
2. 信号在交易日收盘后生成，下一交易日执行，避免使用未来价格；
3. 计入手续费、税费、滑点、现金和股息到账时间；
4. 处理停牌、退市、分红取消、拆分、送转和数据修订；
5. 同时比较买入并持有、固定金额定投和本模型；
6. 报告总收益、股息收入、最大回撤、换手率、现金占比和资金使用率；
7. 对阈值、买入比例和减仓比例做敏感性分析；
8. 使用样本外或滚动窗口验证，不能只挑选表现好的时间段。

V1 第一验收目标不是证明跑赢市场，而是确认：相同数据快照和相同参数会得到相同结果；缺失数据不会生成错误的买入建议；核心仓、预算和交易单位限制始终生效。回测收益只是历史模拟结果，不是未来收益承诺。

## 13. V1 最小验收用例

实现后至少验证以下场景：

| 场景 | 预期结果 |
| --- | --- |
| TTM DPS 缺失、为 0 或价格缺失 | `model_status_code = unavailable`，买卖股数为 0 |
| 四个收益率阈值缺失或顺序错误 | `model_status_code = unavailable`，不生成价格区域 |
| 最近五年数据不足 | `dividend_reliability_code = cautious`，不自动买入 |
| 支付率小于等于 0 或大于等于 1 | `dividend_reliability_code = failed`，不自动买入 |
| 已确认取消分红 | `model_status_code = re_evaluate`，不自动交易 |
| 当前价格进入 `strong_buy` 且预算足够 | 按预算、组合上限和 `trading_lot_size` 向下取整 |
| 当前持股低于核心仓 | `satellite_shares = 0`，不生成普通减仓 |
| 当前价格进入减仓区 | 卖出股数不超过 `held_shares - core_shares` |
| 同一数据快照重复计算 | 结果字段完全一致，参数使用同一个 `model_parameter_set_id` |

## 14. V1 实现顺序

建议按以下顺序实现：

1. 数据快照、股息口径和模型不可用状态；
2. TTM 实际每股股息（TTM DPS）、股息可靠性和五个价格区域；
3. 核心仓、卫星仓、组合预算和可复现的建议股数；
4. 多股票排序、集中度限制和建议快照；
5. 交易费用、历史回放和敏感性分析；
6. 预计未来每股股息（Forward DPS）、用户自定义每股股息和长期情景规划。

V1 不应先实现复杂预测、历史分位数或行业化评分。先保证同一份数据快照和同一组参数永远得到同一个结果，再逐步增加指标。

## 15. 主要参考资料

- [上海证券交易所：关于修订上证红利指数编制方案的公告](https://www.sse.com.cn/market/sseindex/diclosure/c/c_20220327_5700336.shtml)
- [S&P Dow Jones Indices：S&P China Dividend Aristocrats Indices Methodology](https://www.spglobal.com/spdji/tc/documents/methodologies/methodology-sp-china-div-arist-indices.pdf)
- [MSCI：High Dividend Yield Indexes Methodology](https://www.msci.com/indexes/documents/methodology/3_MSCI_High_Dividend_Yield_Indexes_Methodology_20250829.pdf)
- [FTSE Russell：All-World High Dividend Yield Index](https://www.lseg.com/en/ftse-russell/indices/ftse-all-world-high-dividend-yield-index)
- [S&P Dow Jones Indices：Exploring the S&P China A-Share LargeCap High Yield 50 Index](https://www.spglobal.com/spdji/en/education/article/exploring-the-sp-china-a-share-largecap-high-yield-50-index/)
