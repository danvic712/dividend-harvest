import { ArrowDownRight, ArrowRight, Check, Clock3, Coins, ShieldCheck, Target, WalletCards } from "lucide-react"

import { Button } from "@/components/ui/button"
import { formatMoney, formatNumber, formatPercent } from "@/lib/utils"
import type { StockRecommendationResult } from "@/lib/api-types"

export function RecommendationDecision({ recommendation, onRecord }: { recommendation: StockRecommendationResult; onRecord: (direction: "buy" | "sell") => void }) {
  const { analysis } = recommendation
  const isSell = analysis.recommendationCode.toLowerCase().includes("trim")
  const actionShares = isSell ? recommendation.suggestedSellShares : recommendation.suggestedBuyShares
  const headline = headlineFor(analysis.recommendationCode)
  const currentZone = zoneLabel(analysis.priceZoneCode)
  const zoneRows = [
    { code: "strong_buy", label: "强买入区", price: analysis.strongBuyPrice, copy: "收益率达到模型的强买入阈值。", className: "zone-buy" },
    { code: "accumulate", label: "分批加仓区", price: analysis.accumulatePrice, copy: "适合按计划分批记录模拟买入。", className: "zone-accumulate" },
    { code: "hold", label: "观察区", price: analysis.closePrice, copy: "等待新的数据确认，不急于使用预算。", className: "zone-hold" },
    { code: "partial_trim", label: "减仓候选区", price: analysis.partialTrimPrice, copy: "估值进入减仓候选参考范围。", className: "zone-trim" },
    { code: "aggressive_trim", label: "激进减仓区", price: analysis.aggressiveTrimPrice, copy: "只在仓位与规则都允许时考虑。", className: "zone-aggressive" },
  ]

  return (
    <>
      <section className="d-hero">
        <div className="d-story">
          <div className="d-kicker"><span>01</span><span>今天的动作</span><i /></div>
          <div className="d-stock-identity"><div className="d-stock-avatar">{analysis.securityName.slice(0, 2)}</div><div><strong>{analysis.securityName}</strong><span>{analysis.securityCode}.{analysis.exchangeCode} · A 股关注标的</span></div></div>
          <h1>{headline.lead}<span>{headline.accent}</span></h1>
          <p className="d-hero-copy">{analysis.explanation || "模型暂未提供解释，请先同步完整的股票资料。"}</p>
          <div className="d-action-row"><div className="d-action-amount"><div className="d-action-icon"><ArrowDownRight size={17} /></div><div><span>本次建议</span><strong>{actionShares > 0 ? `${isSell ? "卖出" : "买入"} ${formatNumber(actionShares, 0)} 股` : "保持观察"}</strong><small>{actionShares > 0 ? `约 ${formatMoney(recommendation.suggestedTradeAmount)} · 价格区间 ${currentZone}` : "等待新的数据确认"}</small></div></div><Button className="d-primary-button" onClick={() => onRecord(actionShares > 0 ? (isSell ? "sell" : "buy") : "buy")}>{actionShares > 0 ? (isSell ? "记录模拟卖出" : "记录模拟买入") : "记录观察"}<ArrowRight data-icon="inline-end" /></Button></div>
        </div>
        <section className="surface d-signal-card"><div className="d-card-topline"><span>02 / 03 · 信号依据</span><Target size={16} /></div><h2>{signalTitle(analysis.recommendationCode)}</h2><p className="d-card-description">TTM 实际股息 ÷ 当前价格 · 数据截至 {analysis.dataAsOfDate ?? "暂无数据日"}</p><div className="d-readout-grid"><div><span>当前价格</span><strong>{formatMoney(analysis.closePrice)}</strong><em>{analysis.priceZoneConfirmed ? "价格区间已确认" : "价格区间待确认"}</em></div><div><span>TTM 股息率</span><strong>{formatPercent(analysis.dividendYield)}</strong><small>实际股息 {formatMoney(analysis.modelDividendPerShare)} / 股</small></div></div><div className="d-reason-list"><div><span><Check size={12} /></span><p>模型状态：{analysis.modelStatusCode}；股息可靠性：{analysis.dividendReliabilityCode}。</p></div><div><span><Check size={12} /></span><p>当前价格区间：{currentZone}，建议金额由后端预算和持仓规则共同计算。</p></div><div><span><Check size={12} /></span><p>当前持仓 {formatNumber(analysis.heldShares, 0)} 股，其中核心仓 {formatNumber(analysis.coreShares, 0)} 股。</p></div></div><div className="d-data-foot"><Clock3 size={13} /> 数据来自 FTShare，按交易日更新</div></section>
      </section>
      <section className="d-ladder-section"><div className="d-section-head"><div><div className="d-kicker"><span>02</span><span>价格阶梯</span><i /></div><h2>每个价位，买不同的分量。</h2><p>把“可以买”翻译成下一步能执行的动作。</p></div><div className="d-current-price"><span>当前价格</span><strong>{formatMoney(analysis.closePrice)}</strong><small>落在「{currentZone}」</small></div></div><div className="surface d-ladder-card"><div className="d-zone-grid">{zoneRows.map((zone) => <div className={`d-zone-card ${zone.className} ${analysis.priceZoneCode === zone.code ? "d-zone-card-active" : ""}`} key={zone.code}><span>{zone.label}</span><strong>{formatMoney(zone.price)}</strong><small>{zone.copy}</small></div>)}</div><div className="d-ladder-caption"><span><b />蓝色是重点买入，琥珀色是小步加仓。</span><span>模型参数驱动</span></div></div></section>
      <section className="d-plan-grid"><div className="surface d-plan-card"><div className="d-card-topline"><span>03 / 03 · 执行计划</span><Coins size={16} /></div><h2>下一步怎么做</h2><p className="d-card-description">{analysis.securityName} 单独配置，核心仓不动。</p><div className="d-plan-list"><div className="d-plan-row"><span className="d-plan-number d-plan-add">01</span><div><strong>当前区间</strong><span>{currentZone}</span></div><div className="d-plan-action"><b>{actionShares > 0 ? `${isSell ? "卖出" : "买入"} ${formatNumber(actionShares, 0)} 股` : "保持观察"}</b><small>{formatMoney(recommendation.suggestedTradeAmount)}</small></div><Check size={15} /></div><div className="d-plan-row"><span className="d-plan-number d-plan-strong">02</span><div><strong>下一档</strong><span>模型继续确认</span></div><div className="d-plan-action"><b>继续按阶梯</b><small>不一次性用完预算</small></div><Check size={15} /></div><div className="d-plan-row"><span className="d-plan-number d-plan-hold">03</span><div><strong>保护线</strong><span>核心仓 {formatNumber(analysis.coreShares, 0)} 股</span></div><div className="d-plan-action"><b>不参与波段</b><small>重大变化时重新评估</small></div><Check size={15} /></div></div><div className="d-plan-note"><Coins size={15} /><span>建议金额随价格、预算和持仓变化；它不是自动交易指令。</span></div></div><div className="surface d-position-card"><div className="d-card-topline"><span>你的仓位</span><WalletCards size={16} /></div><h2>核心仓，先保护好。</h2><p className="d-card-description">{analysis.securityName} · 当前持仓摘要</p><div className="d-position-hero"><strong>{formatNumber(analysis.heldShares, 0)}</strong><span> 股总持仓</span><b>{formatNumber(analysis.coreShares, 0)} 股核心仓</b></div><div className="d-position-meta"><div><span>核心仓</span><strong>{formatNumber(analysis.coreShares, 0)} 股</strong></div><div><span>卫星仓</span><strong>{formatNumber(analysis.satelliteShares, 0)} 股</strong></div><div><span>区间状态</span><strong>{analysis.priceZoneConfirmed ? "已确认" : "待确认"}</strong></div></div></div></section>
      <section className="d-bottom-grid"><div className="surface d-dividend-card"><div className="d-bottom-heading"><div><span>现金流证据</span><strong>TTM 实际股息</strong></div><div className="d-dividend-total"><b>{formatMoney(analysis.modelDividendPerShare)}</b><small>/ 股 · 数据日 {analysis.dataAsOfDate ?? "—"}</small></div></div><div className="d-dividend-line"><div><span>股息模式</span><strong>{analysis.dividendModeCode ?? "—"}</strong><small>后端规范化结果</small></div><div><span>可靠性</span><strong>{analysis.dividendReliabilityCode}</strong><small>不使用缺失数据补齐</small></div><div><span>模型状态</span><strong>{analysis.modelStatusCode}</strong><small>来源于当前分析</small></div></div></div><div className="d-trust-note"><ShieldCheck size={20} /><div><strong>这是一份带日期的交易参考</strong><p>如果实际股息资料缺失，系统不会生成买入信号。请结合自己的判断。</p></div></div></section>
    </>
  )
}

function headlineFor(code: string) {
  const normalized = code.toLowerCase()
  if (normalized === "strong_buy") return { lead: "可以", accent: "试买" }
  if (normalized === "accumulate") return { lead: "可以", accent: "加仓" }
  if (normalized.includes("trim")) return { lead: "考虑", accent: "减仓" }
  if (normalized === "re_evaluate") return { lead: "需要", accent: "复核" }
  if (normalized === "no_action") return { lead: "暂不", accent: "动作" }
  return { lead: "先", accent: "观察" }
}

function signalTitle(code: string) {
  const normalized = code.toLowerCase()
  if (normalized === "strong_buy") return "价格进入试探区间"
  if (normalized === "accumulate") return "收益率回到可接受区间"
  if (normalized.includes("trim")) return "收益率偏低，先保护仓位"
  if (normalized === "re_evaluate") return "资料需要重新确认"
  if (normalized === "no_action") return "当前没有需要执行的动作"
  return "等待收益率重新抬升"
}

function zoneLabel(code: string | null) {
  return ({ strong_buy: "强买入区", accumulate: "分批加仓区", hold: "观察区", partial_trim: "减仓候选区", aggressive_trim: "激进减仓区" } as Record<string, string>)[code ?? ""] ?? "待确认"
}
