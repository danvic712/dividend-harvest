# 成熟股息收益策略研究摘要

更新时间：2026-09-01

## 结论摘要

成熟的股息指数编制方法通常不是简单选择股息率最高的股票，而是把以下几层规则组合起来：

1. 先确认股票具备连续、真实的现金分红记录；
2. 排除股利支付率极端、盈利为负或分红可能无法持续的公司；
3. 再按股息率、股息增长或相对历史区间选择股票；
4. 同时考虑盈利能力、负债、价格下跌风险、流动性和组合集中度；
5. 通过权重上限、缓冲区和定期调整降低换手和集中风险。

## 参考方法

### 上证红利

上海证券交易所 2022 年公告中的修订方案包含三个对本项目有用的设计：

- 过去三年连续现金分红；
- 过去三年平均股利支付率和过去一年股利支付率均大于 0 且小于 1；
- 按过去三年平均现金股息率排序选取样本，并设置个股权重上限。

来源：[上海证券交易所关于修订上证红利指数编制方案的公告](https://www.sse.com.cn/market/sseindex/diclosure/c/c_20220327_5700336.shtml)

对 Dividend Harvest 的启示：不能只看最近一次分红或当天股息率，应同时检查多年分红记录和支付能力。

### S&P 中国 A 股高股息红利贵族

S&P 中国 A 股高股息红利贵族方法要求股票至少连续五年增加或维持股息，同时检查市值和流动性；通过筛选后，再按过去十二个月股息率排序，并将单只股票权重限制在 5% 以内。

来源：[S&P China Dividend Aristocrats Indices Methodology](https://www.spglobal.com/spdji/tc/documents/methodologies/methodology-sp-china-div-arist-indices.pdf)

对 Dividend Harvest 的启示：股息持续性、可交易性和组合上限应成为独立的筛选层，不能全部混在“目标股息率”里。

### MSCI 高股息指数

MSCI 的高股息方法包含以下过滤：

- 排除股利支付率为负或极端高的股票；
- 排除五年每股股息增长为负的股票；
- 使用 ROE、盈利波动和负债等指标进行质量筛选；
- 排除价格表现极端弱势的股票；
- 要求股息率相对于母指数达到一定的相对水平；
- 对发行人设置权重上限以控制集中风险。

来源：[MSCI High Dividend Yield Indexes Methodology](https://www.msci.com/indexes/documents/methodology/3_MSCI_High_Dividend_Yield_Indexes_Methodology_20250829.pdf)

对 Dividend Harvest 的启示：高股息可能只是股价大跌的结果，因此股息率信号必须经过可持续性、质量和下跌风险检查。

### FTSE All-World High Dividend Yield

FTSE All-World High Dividend Yield 方法使用未来十二个月股息率，对股票进行相对排名；预计未来十二个月不分红的股票会被排除，特别分红不计入常规股息，并使用流动性筛选和成分缓冲规则减少不必要的进出。

来源：[FTSE Russell All-World High Dividend Yield Index](https://www.lseg.com/en/ftse-russell/indices/ftse-all-world-high-dividend-yield-index)

对 Dividend Harvest 的启示：预计股息和特别股息必须单独标记；信号不应因股息率在边界附近的小变化而频繁跳转。

## 3. 外部方法到本项目 V1 的映射

本项目是个人 A 股工具，不需要复制机构指数的全部复杂细节。V1 只吸收容易解释、容易验证的规则：

| 外部设计 | 本项目 V1 采用方式 | 暂不采用的复杂部分 |
| --- | --- | --- |
| 连续现金分红 | 最近三年每年都有已实施常规现金分红 | 全市场样本池维护 |
| 股息稳定性 | 最近五个完整年度数据齐全，最新 DPS 不低于最早年度 DPS | 复杂的股息贵族分级 |
| 支付能力 | 最新支付率和三年平均支付率都满足 `0 < ratio < 1` | 多套行业支付率模型 |
| 股息率选择 | 每只股票手工配置四个收益率阈值 | V1 不自动拟合 Q20/Q40/Q60/Q80 |
| 预计股息 | 默认不参与实时建议，用户明确开启后才使用 | 自动预测未来股息 |
| 质量 | ROE、盈利、负债、P/B（PB）作为提示 | 三项评分、行业化复杂阈值 |
| 价格风险 | 异常下跌显示谨慎提醒 | 复杂动量和风险因子 |
| 组合风险 | 单只股票、行业、现金和单次交易上限 | 指数级动态权重优化 |
| 换手控制 | 收盘计算、连续两日确认新价格区域 | 复杂再平衡缓冲算法 |

## 4. V1 推荐规则

```text
1. 读取截至收盘日已经公开的数据
2. 汇总最近 12 个月已实施的常规 TTM DPS
3. 检查三年连续分红、五年股息趋势和支付率
4. 用 D_model ÷ 当前价格计算股息率
5. 与每只股票配置的四个阈值比较，得到价格区域
6. 应用预算、组合上限、核心仓和交易单位
7. 输出建议股数、原因、数据日期和参数版本
```

V1 的状态规则：

```text
数据不完整或模型股息无效 → unavailable
明确取消分红、支付率失控或股息趋势明确恶化 → failed / re_evaluate
历史数据不足但无法确认恶化 → cautious，不自动买入
全部硬性检查通过 → available，允许按价格区域给建议
```

## 5. 为什么这样简化

个人工具最容易犯的错误是把“看起来专业的指标”叠加在一起，却无法解释某次建议为什么产生。V1 先确保以下事实可追溯：

- 这笔模型股息是哪几笔已实施分红相加得到的；
- 计算时使用的是哪一天的价格和数据；
- 为什么股票被判定为可用、谨慎或不可用；
- 为什么进入某个价格区域；
- 为什么最终只能买入或卖出这么多股。

只有这些规则稳定、回测能够复现后，才考虑加入 Forward DPS、历史分位数、行业化质量筛选和更复杂的组合优化。

## 6. 限制与风险

- 指数方法论的历史表现不能证明本项目的个人策略未来有效；
- 股息率上升可能由股价下跌造成，不代表股票更安全；
- 已宣布分红可能被修改或取消；
- 税费、手续费、滑点和到账时间会影响实际收益；
- A 股行业差异明显，统一阈值不适合所有股票；
- 回测必须使用当时可获得的数据，不能使用未来修订值；
- 本方案只提供研究和持仓管理参考，不构成投资建议，也不自动执行交易。

## 7. 主要来源

- [上海证券交易所：关于修订上证红利指数编制方案的公告](https://www.sse.com.cn/market/sseindex/diclosure/c/c_20220327_5700336.shtml)
- [S&P Dow Jones Indices：S&P China Dividend Aristocrats Indices Methodology](https://www.spglobal.com/spdji/tc/documents/methodologies/methodology-sp-china-div-arist-indices.pdf)
- [MSCI：High Dividend Yield Indexes Methodology](https://www.msci.com/indexes/documents/methodology/3_MSCI_High_Dividend_Yield_Indexes_Methodology_20250829.pdf)
- [FTSE Russell：All-World High Dividend Yield Index](https://www.lseg.com/en/ftse-russell/indices/ftse-all-world-high-dividend-yield-index)
