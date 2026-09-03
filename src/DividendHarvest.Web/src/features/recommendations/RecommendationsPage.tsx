import { AlertCircle, ChevronRight, Clock3, Database, RefreshCw, Save, ShieldCheck } from "lucide-react"
import { useCallback, useEffect, useMemo, useState } from "react"

import { AppShell, SectionHeading } from "@/components/app-shell"
import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { StockSelector } from "@/components/stock-selector"
import { getApiErrorMessage } from "@/lib/api-errors"
import { displayStockName, hasAnalysisData } from "@/lib/stock-display"
import { formatDateTime, formatMoney, formatNumber, stockKey } from "@/lib/utils"
import type { BudgetSummary, PortfolioRecommendationResult, StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"
import { createRecommendationSnapshot, getBudgetSummary, getRecommendations, getWatchlist } from "@/features/recommendations/recommendations.api"
import { RecommendationDecision } from "@/features/recommendations/RecommendationDecision"
import "./recommendations.css"

type RecommendationsPageProps = {
  onNavigate: (path: string) => void
  notice?: string | null
}

export function RecommendationsPage({ onNavigate, notice }: RecommendationsPageProps) {
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [recommendation, setRecommendation] = useState<PortfolioRecommendationResult | null>(null)
  const [budget, setBudget] = useState<BudgetSummary | null>(null)
  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [recommendationError, setRecommendationError] = useState<string | null>(null)
  const [savingSnapshot, setSavingSnapshot] = useState(false)
  const [snapshotMessage, setSnapshotMessage] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    setRecommendationError(null)
    const [watchlistResult, recommendationResult, budgetResult] = await Promise.allSettled([getWatchlist(), getRecommendations(), getBudgetSummary()])

    if (watchlistResult.status === "rejected") {
      setError(getApiErrorMessage(watchlistResult.reason, "今日决策暂时无法读取，请检查后端服务。"))
      setLoading(false)
      return
    }

    const watchlist = watchlistResult.value
    setStocks(watchlist)
    setSelectedKey((current) => current && watchlist.some((stock) => stockKey(stock) === current) ? current : watchlist[0] ? stockKey(watchlist[0]) : null)

    if (recommendationResult.status === "fulfilled") {
      setRecommendation(recommendationResult.value)
    } else {
      setRecommendation(null)
      setRecommendationError(getApiErrorMessage(recommendationResult.reason, "股票资料仍在后台同步，交易参考暂未生成。"))
    }

    if (budgetResult.status === "fulfilled") {
      setBudget(budgetResult.value)
    } else {
      setBudget(null)
    }

    setLoading(false)
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])

  const selectedRecommendation = useMemo<StockRecommendationResult | null>(() => recommendation?.stocks.find((item) => stockKey(item.analysis) === selectedKey) ?? null, [recommendation, selectedKey])
  const readyRecommendation = recommendation?.stocks.some((item) => hasAnalysisData(item.analysis)) ? recommendation : null
  const hasReadyRecommendation = Boolean(readyRecommendation)
  const selectedRecommendationReady = hasAnalysisData(selectedRecommendation?.analysis)
  const selectedStock = stocks.find((stock) => stockKey(stock) === selectedKey)
  const lastUpdated = recommendation?.computedAt ?? budget?.computedAt

  async function saveSnapshot() {
    setSavingSnapshot(true)
    setSnapshotMessage(null)
    try {
      await createRecommendationSnapshot()
      setSnapshotMessage("已保存本次组合建议快照。")
    } catch (snapshotError) {
      setSnapshotMessage(getApiErrorMessage(snapshotError, "快照保存失败，请稍后重试。"))
    } finally {
      setSavingSnapshot(false)
    }
  }

  if (loading) return <div className="page-wrap"><LoadingState label="正在读取今日组合决策…" /></div>
  if (error && !stocks.length) return <div className="page-wrap"><ErrorState message={error} onRetry={() => void load()} /></div>
  if (!stocks.length) return <div className="page-wrap"><EmptyState title="还没有股票资料" description="先完成组合初始化，系统才能生成每只股票的价格区间和交易参考。" action={<Button onClick={() => onNavigate("/setup")}>去建立组合</Button>} /></div>

  return (
    <AppShell currentPath="/overview" onNavigate={onNavigate} lastUpdated={lastUpdated ? formatDateTime(lastUpdated) : null} dataState={hasReadyRecommendation ? "synced" : "pending"}>
      <div className="d-breadcrumb"><span>我的股票</span><ChevronRight size={13} /><strong>{selectedStock ? displayStockName(selectedStock) : "今日决策"}</strong><span>{selectedStock ? `${selectedStock.securityCode}.${selectedStock.exchangeCode} · A 股` : "A 股参考"}</span><div className="d-overview-actions"><Button variant="outline" size="sm" onClick={() => void load()}><RefreshCw data-icon="inline-start" />刷新资料</Button><Button variant="secondary" size="sm" onClick={() => void saveSnapshot()} disabled={savingSnapshot || !hasReadyRecommendation}><Save data-icon="inline-start" />{savingSnapshot ? "保存中…" : "保存快照"}</Button></div></div>
      {notice && <Alert variant="attention" className="d-inline-alert"><AlertTitle>资料同步状态</AlertTitle><AlertDescription>{notice}</AlertDescription></Alert>}
      {error && <Alert variant="destructive" className="d-inline-alert"><AlertTitle>读取提醒</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      <StockSelector stocks={stocks} recommendations={recommendation?.stocks ?? []} selectedKey={selectedKey} onSelect={(stock) => setSelectedKey(stockKey(stock))} />
      {readyRecommendation ? <>
        <div className="surface recommendation-summary"><div><p className="eyebrow">PORTFOLIO PULSE</p><strong className="summary-number">{formatMoney(readyRecommendation.totalSuggestedTradeAmount)}</strong><p className="muted-copy">当前组合建议记录金额 · {readyRecommendation.stocks.length} 只股票</p></div><div className="summary-side"><div className="summary-side-item"><span>可用预算</span><strong>{formatMoney(readyRecommendation.remainingAvailableBudgetAmount)}</strong></div><div className="summary-side-item"><span>预计费用</span><strong>{formatMoney(readyRecommendation.estimatedTransactionFeeAmount)}</strong></div><div className="summary-side-item"><span>现金余额</span><strong>{formatMoney(budget?.cashBalanceAmount)}</strong></div></div></div>
        {snapshotMessage && <p className="form-message d-inline-message">{snapshotMessage}</p>}
        {selectedRecommendation && selectedRecommendationReady ? <RecommendationDecision recommendation={selectedRecommendation} onRecord={(direction) => onNavigate(`/portfolio?stock=${encodeURIComponent(stockKey(selectedRecommendation.analysis))}&direction=${direction}`)} /> : selectedStock ? <PendingDecisionView selectedStock={selectedStock} message="这只股票的资料仍在后台同步，完成后会单独生成价格阶梯。" onRefresh={() => void load()} onNavigate={onNavigate} budget={budget} /> : <EmptyState title="这只股票还没有建议" description="可能是股票资料尚未同步完成，先到股票资料页执行同步。" action={<Button onClick={() => onNavigate("/stocks")}>查看股票资料</Button>} />}
        <section className="analysis-section"><SectionHeading label="COMPOSITION" title="组合中的每个决定，都有来源" description="建议金额由后端模型根据预算、持仓和单股限制计算。页面只呈现结果，不在前端重复计算口径。" /><div className="decision-support"><div className="support-tile"><span>组合可用预算</span><strong>{formatMoney(readyRecommendation.startingAvailableBudgetAmount)}</strong></div><div className="support-tile"><span>建议涉及股数</span><strong>{formatNumber(readyRecommendation.stocks.reduce((total, item) => total + item.suggestedBuyShares + item.suggestedSellShares, 0), 0)} 股</strong></div><div className="support-tile"><span>组合计算时间</span><strong>{formatDateTime(readyRecommendation.computedAt)}</strong></div></div></section>
      </> : <PendingDecisionView selectedStock={selectedStock} message={recommendationError} onRefresh={() => void load()} onNavigate={onNavigate} budget={budget} />}
    </AppShell>
  )
}

function PendingDecisionView({ selectedStock, message, onRefresh, onNavigate, budget }: { selectedStock?: StockWatchlistItem; message: string | null; onRefresh: () => void; onNavigate: (path: string) => void; budget: BudgetSummary | null }) {
  const stockName = selectedStock ? displayStockName(selectedStock) : "你的股票"
  return <>
    <section className="d-hero d-pending-hero">
      <div className="d-story"><div className="d-kicker"><span>01</span><span>今天的动作</span><i /></div><div className="d-stock-identity"><div className="d-stock-avatar">{stockName.slice(0, 2)}</div><div><strong>{stockName}</strong><span>{selectedStock ? `${selectedStock.securityCode}.${selectedStock.exchangeCode} · A 股关注标的` : "A 股关注标的"}</span></div></div><h1>资料<span>在路上</span></h1><p className="d-hero-copy">FTShare 正在后台补齐股票基础资料、行情、股息和财务快照。同步完成后，系统会根据每只股票的参数生成价格阶梯和交易参考。</p><div className="d-action-row"><div className="d-action-amount"><div className="d-action-icon d-pending-icon"><RefreshCw size={17} className="spin" /></div><div><span>当前状态</span><strong>等待同步完成</strong><small>{message ?? "不会阻塞你的组合使用"}</small></div></div><Button className="d-primary-button" onClick={onRefresh}>重新检查<ArrowRightIcon /></Button></div></div>
      <section className="surface d-signal-card d-pending-card"><div className="d-card-topline"><span>02 / 03 · 信号依据</span><Database size={16} /></div><h2>先不生成买入信号</h2><p className="d-card-description">没有完整的实际股息和行情，就没有可解释的价格区间。</p><div className="d-pending-status-list"><PendingStatus label="基础资料" detail="等待 FTShare 返回" /><PendingStatus label="TTM 实际股息" detail="不使用缺失数据补齐" /><PendingStatus label="价格阶梯" detail="资料完整后计算" /></div><div className="d-data-foot"><Clock3 size={13} /> 你可以先补充持仓和现金流水</div></section>
    </section>
    <section className="d-pending-grid"><div className="surface d-pending-next"><div className="d-card-topline"><span>03 / 03 · 现在可以做什么</span><ShieldCheck size={16} /></div><h2>先把组合准备好。</h2><p>保存已经完成，后台同步不影响你继续记录自己的持仓和预算。</p><div className="d-pending-actions"><Button variant="outline" onClick={() => onNavigate("/portfolio")}>补充持仓记录<ChevronRight data-icon="inline-end" /></Button><Button variant="outline" onClick={() => onNavigate("/budget")}>记录预算流水<ChevronRight data-icon="inline-end" /></Button></div></div><div className="surface d-pending-budget"><span>可用现金余额</span><strong>{formatMoney(budget?.cashBalanceAmount)}</strong><small>建议金额会在同步完成后计算</small></div></section>
  </>
}

function PendingStatus({ label, detail }: { label: string; detail: string }) {
  return <div className="d-pending-status"><span className="d-pending-status-icon"><AlertCircle size={14} /></span><div><strong>{label}</strong><small>{detail}</small></div><span className="d-pending-status-label">等待中</span></div>
}

function ArrowRightIcon() {
  return <ChevronRight data-icon="inline-end" />
}
