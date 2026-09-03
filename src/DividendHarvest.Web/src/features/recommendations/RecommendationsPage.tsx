import { ChevronRight, RefreshCw, Save } from "lucide-react"
import { useCallback, useEffect, useMemo, useState } from "react"

import { AppShell, SectionHeading } from "@/components/app-shell"
import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { StockSelector } from "@/components/stock-selector"
import { getApiErrorMessage } from "@/lib/api-errors"
import { formatDateTime, formatMoney, formatNumber, stockKey } from "@/lib/utils"
import type { BudgetSummary, PortfolioRecommendationResult, StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"
import { createRecommendationSnapshot, getBudgetSummary, getRecommendations, getWatchlist } from "@/features/recommendations/recommendations.api"
import { RecommendationDecision } from "@/features/recommendations/RecommendationDecision"
import "./recommendations.css"

export function RecommendationsPage({ onNavigate, notice }: { onNavigate: (path: string) => void; notice?: string | null }) {
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [recommendation, setRecommendation] = useState<PortfolioRecommendationResult | null>(null)
  const [budget, setBudget] = useState<BudgetSummary | null>(null)
  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [savingSnapshot, setSavingSnapshot] = useState(false)
  const [snapshotMessage, setSnapshotMessage] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [watchlist, recommendations, budgetSummary] = await Promise.all([getWatchlist(), getRecommendations(), getBudgetSummary()])
      setStocks(watchlist)
      setRecommendation(recommendations)
      setBudget(budgetSummary)
      setSelectedKey((current) => current && watchlist.some((stock) => stockKey(stock) === current) ? current : watchlist[0] ? stockKey(watchlist[0]) : null)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "今日决策暂时无法读取，请检查后端服务。"))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])

  const selectedRecommendation = useMemo<StockRecommendationResult | null>(() => recommendation?.stocks.find((item) => stockKey(item.analysis) === selectedKey) ?? null, [recommendation, selectedKey])
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
  if (error) return <div className="page-wrap"><ErrorState message={error} onRetry={() => void load()} /></div>
  if (!stocks.length || !recommendation) return <div className="page-wrap"><EmptyState title="还没有股票资料" description="先完成组合初始化，系统才能生成每只股票的价格区间和交易参考。" action={<Button onClick={() => onNavigate("/setup")}>去建立组合</Button>} /></div>

  return (
    <AppShell currentPath="/overview" onNavigate={onNavigate} lastUpdated={lastUpdated ? formatDateTime(lastUpdated) : null}>
      <div className="d-breadcrumb"><span>我的股票</span><ChevronRight size={13} /><strong>{selectedStock?.securityName ?? "今日决策"}</strong><span>{selectedStock ? `${selectedStock.securityCode}.${selectedStock.exchangeCode} · A 股` : "A 股参考"}</span><div className="d-overview-actions"><Button variant="outline" size="sm" onClick={() => void load()}><RefreshCw data-icon="inline-start" />刷新资料</Button><Button variant="secondary" size="sm" onClick={() => void saveSnapshot()} disabled={savingSnapshot}><Save data-icon="inline-start" />{savingSnapshot ? "保存中…" : "保存快照"}</Button></div></div>
      {notice && <Alert variant="attention" style={{ marginBottom: 16 }}><AlertTitle>资料同步状态</AlertTitle><AlertDescription>{notice}</AlertDescription></Alert>}
      <StockSelector stocks={stocks} recommendations={recommendation.stocks} selectedKey={selectedKey} onSelect={(stock) => setSelectedKey(stockKey(stock))} />
      <div className="surface recommendation-summary"><div><p className="eyebrow">PORTFOLIO PULSE</p><strong className="summary-number">{formatMoney(recommendation.totalSuggestedTradeAmount)}</strong><p className="muted-copy">当前组合建议记录金额 · {recommendation.stocks.length} 只股票</p></div><div className="summary-side"><div className="summary-side-item"><span>可用预算</span><strong>{formatMoney(recommendation.remainingAvailableBudgetAmount)}</strong></div><div className="summary-side-item"><span>预计费用</span><strong>{formatMoney(recommendation.estimatedTransactionFeeAmount)}</strong></div><div className="summary-side-item"><span>现金余额</span><strong>{formatMoney(budget?.cashBalanceAmount)}</strong></div></div></div>
      {snapshotMessage && <p className="form-message" style={{ margin: "12px 0 0" }}>{snapshotMessage}</p>}
      {selectedRecommendation ? <RecommendationDecision recommendation={selectedRecommendation} onRecord={(direction) => onNavigate(`/portfolio?stock=${encodeURIComponent(stockKey(selectedRecommendation.analysis))}&direction=${direction}`)} /> : <EmptyState title="这只股票还没有建议" description="可能是股票资料尚未同步完成，先到股票资料页执行同步。" action={<Button onClick={() => onNavigate("/stocks")}>查看股票资料</Button>} />}
      <section className="analysis-section"><SectionHeading label="COMPOSITION" title="组合中的每个决定，都有来源" description="建议金额由后端模型根据预算、持仓和单股限制计算。页面只呈现结果，不在前端重复计算口径。" /><div className="decision-support"><div className="support-tile"><span>组合可用预算</span><strong>{formatMoney(recommendation.startingAvailableBudgetAmount)}</strong></div><div className="support-tile"><span>建议涉及股数</span><strong>{formatNumber(recommendation.stocks.reduce((total, item) => total + item.suggestedBuyShares + item.suggestedSellShares, 0), 0)} 股</strong></div><div className="support-tile"><span>组合计算时间</span><strong>{formatDateTime(recommendation.computedAt)}</strong></div></div></section>
    </AppShell>
  )
}
