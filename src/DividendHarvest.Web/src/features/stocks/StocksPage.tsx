import { Database, RefreshCw } from "lucide-react"
import { useCallback, useEffect, useState } from "react"

import { AppShell, PageTitle, SectionHeading } from "@/components/app-shell"
import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { StockSelector } from "@/components/stock-selector"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { getApiErrorMessage } from "@/lib/api-errors"
import { formatDate, formatDateTime, formatMoney, formatPercent, stockKey } from "@/lib/utils"
import type { StockAnalysisResult, StockDataSyncRunResult, StockModelParameterSet, StockWatchlistItem } from "@/lib/api-types"
import { getModelParameters, getStockAnalysis, getStocks, syncStocks } from "@/features/stocks/stocks.api"
import "./stocks.css"

export function StocksPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [analysis, setAnalysis] = useState<StockAnalysisResult | null>(null)
  const [parameters, setParameters] = useState<StockModelParameterSet | null>(null)
  const [loading, setLoading] = useState(true)
  const [detailLoading, setDetailLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState<StockDataSyncRunResult | null>(null)

  const loadStocks = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const list = await getStocks()
      setStocks(list)
      setSelectedKey((current) => current && list.some((stock) => stockKey(stock) === current) ? current : list[0] ? stockKey(list[0]) : null)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "股票资料暂时无法读取。"))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void loadStocks() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [loadStocks])

  const loadDetail = useCallback(async (signal?: AbortSignal) => {
    const stock = stocks.find((item) => stockKey(item) === selectedKey)
    if (!stock) {
      setAnalysis(null)
      setParameters(null)
      return
    }
    setDetailLoading(true)
    setError(null)
    try {
      const [analysisResult, parameterResult] = await Promise.all([getStockAnalysis(stock.securityCode, stock.exchangeCode, signal), getModelParameters(stock.securityCode, stock.exchangeCode, signal)])
      if (!signal?.aborted) {
        setAnalysis(analysisResult.analysis)
        setParameters(parameterResult)
      }
    } catch (detailError) {
      if (!signal?.aborted) {
        setError(getApiErrorMessage(detailError, "这只股票的分析资料暂时无法读取。"))
      }
    } finally {
      if (!signal?.aborted) {
        setDetailLoading(false)
      }
    }
  }, [selectedKey, stocks])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => { void loadDetail(controller.signal) }, 0)
    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadDetail])

  async function syncAll() {
    setSyncing(true)
    setSyncResult(null)
    setError(null)
    try {
      setSyncResult(await syncStocks())
      await loadStocks()
    } catch (syncError) {
      setError(getApiErrorMessage(syncError, "资料同步失败，请稍后重试。"))
    } finally {
      setSyncing(false)
    }
  }

  if (loading) return <div className="page-wrap"><LoadingState label="正在读取股票资料…" /></div>
  if (error && !stocks.length) return <div className="page-wrap"><ErrorState message={error} onRetry={() => void loadStocks()} /></div>
  if (!stocks.length) return <div className="page-wrap"><EmptyState title="还没有股票资料" description="完成首次设置后，这里会展示每只股票的资料、持仓和模型参数。" action={<Button onClick={() => onNavigate("/setup")}>去建立组合</Button>} /></div>

  const selectedStock = stocks.find((stock) => stockKey(stock) === selectedKey) ?? stocks[0]
  return (
    <AppShell currentPath="/stocks" onNavigate={onNavigate} lastUpdated={analysis?.computedAt ? formatDateTime(analysis.computedAt) : null}>
      <PageTitle eyebrow="STOCK LIBRARY / SOURCE DATA" title="股票资料，保持可追溯。" description="每个价格、股息和财务快照都来自后端同步结果。缺失资料会明确显示，不会用前端默认值掩盖。" actions={<Button onClick={() => void syncAll()} disabled={syncing}><RefreshCw data-icon="inline-start" />{syncing ? "同步中…" : "同步全部资料"}</Button>} />
      {error && <Alert variant="destructive" style={{ marginBottom: 16 }}><AlertTitle>读取提醒</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      <div className="stocks-layout">
        <section className="surface stock-directory"><SectionHeading label="WATCHLIST" title={`${stocks.length} 只 A 股`} description="按股票单独查看模型输入和结果。" /><StockSelector stocks={stocks} selectedKey={selectedKey} onSelect={(stock) => setSelectedKey(stockKey(stock))} /></section>
        <section className="surface stock-detail">
          {detailLoading ? <LoadingState label="正在读取这只股票的分析…" /> : selectedStock && analysis ? <><div className="stock-detail-header"><div><p className="eyebrow">{analysis.exchangeCode} / {analysis.modelStatusCode}</p><h2 className="stock-detail-name">{analysis.securityName}</h2><p className="stock-detail-code">{analysis.securityCode} · 数据日 {formatDate(analysis.dataAsOfDate)}</p></div><Badge variant={analysis.priceZoneConfirmed ? "accent" : "attention"}>{analysis.priceZoneConfirmed ? "价格区间已确认" : "价格区间待确认"}</Badge></div><div className="stock-analysis-grid"><div className="analysis-tile"><span>最新收盘</span><strong>{formatMoney(analysis.closePrice)}</strong></div><div className="analysis-tile"><span>TTM 股息率</span><strong>{formatPercent(analysis.dividendYield)}</strong></div><div className="analysis-tile"><span>模型股息 / 股</span><strong>{formatMoney(analysis.modelDividendPerShare)}</strong></div><div className="analysis-tile"><span>持仓股数</span><strong>{analysis.heldShares.toLocaleString("zh-CN")}</strong></div><div className="analysis-tile"><span>核心股数</span><strong>{analysis.coreShares.toLocaleString("zh-CN")}</strong></div><div className="analysis-tile"><span>卫星股数</span><strong>{analysis.satelliteShares.toLocaleString("zh-CN")}</strong></div></div><SectionHeading label="MODEL PARAMETERS" title="这只股票的参数" description="模型参数按股票独立保存，修改后由下一次分析读取。" />{parameters ? <div className="parameter-grid">{parameterEntries(parameters).map(([label, value]) => <div className="parameter-item" key={label}><span>{label}</span><strong>{value}</strong></div>)}</div> : <EmptyState title="还没有模型参数" description="请先为这只股票建立参数，后端才会生成完整价格阶梯。" action={<Button variant="outline" onClick={() => onNavigate(`/settings?stock=${encodeURIComponent(stockKey(selectedStock))}`)}>去配置参数</Button>} />}<div className="note-box">{analysis.explanation || "当前没有可展示的模型解释。"}</div></> : <EmptyState title="还没有分析结果" description="先同步资料，或检查后端是否已经为这只股票建立模型参数。" />}
        </section>
      </div>
      {syncResult && <section className="sync-results"><Alert variant={syncResult.partiallyFailedStockCount ? "attention" : "default"}><AlertTitle><Database size={15} style={{ verticalAlign: "-3px", marginRight: 6 }} />资料同步完成</AlertTitle><AlertDescription>尝试 {syncResult.attemptedStockCount} 只，完整完成 {syncResult.fullyCompletedStockCount} 只，部分失败 {syncResult.partiallyFailedStockCount} 只。{syncResult.failures.length ? `失败项 ${syncResult.failures.map((failure) => `${failure.securityCode} ${failure.dataKind}`).join("、")}。` : ""}</AlertDescription></Alert></section>}
    </AppShell>
  )
}

function parameterEntries(parameters: StockModelParameterSet) {
  return [["模型版本", parameters.modelVersion], ["强买入收益率", formatPercent(parameters.strongBuyYieldThreshold)], ["加仓收益率", formatPercent(parameters.accumulationYieldThreshold)], ["减仓候选收益率", formatPercent(parameters.partialTrimYieldThreshold)], ["激进减仓收益率", formatPercent(parameters.aggressiveTrimYieldThreshold)], ["单股最大权重", formatPercent(parameters.maxSecurityWeight)], ["行业最大权重", formatPercent(parameters.maxSectorWeight)], ["现金保留比例", formatPercent(parameters.cashReserveRatio)], ["单次交易上限", formatMoney(parameters.maxSingleTradeAmount)], ["交易单位", `${parameters.tradingLotSize} 股`], ["生效日期", formatDate(parameters.effectiveFromDate)]] as const
}
