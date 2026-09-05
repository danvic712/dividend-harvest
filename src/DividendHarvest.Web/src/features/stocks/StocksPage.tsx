import { Database, RefreshCw } from "lucide-react"
import { useCallback, useEffect, useRef, useState } from "react"

import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { PageFrame } from "@/components/page-frame"
import { PageTitle, SectionHeading } from "@/components/page-heading"
import { PriceLadder, PriceZoneBoard } from "@/components/price-ladder"
import { StockSelector } from "@/components/stock-selector"
import { StockActionSummary } from "@/features/stocks/StockActionSummary"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { getApiErrorMessage } from "@/lib/api-errors"
import { interpolate, useLocale } from "@/lib/i18n"
import { displayStockName, exchangeLabel, hasAnalysisData } from "@/lib/stock-display"
import { formatDate, formatDateTime, formatMoney, formatPercent, stockKey } from "@/lib/utils"
import type { StockAnalysisResult, StockDataSyncRunResult, StockModelParameterSet, StockWatchlistItem } from "@/lib/api-types"
import { getModelParameters, getStockAnalysis, getStocks, syncStocks } from "@/features/stocks/stocks.api"
import { StockDetailSkeleton } from "@/features/stocks/StockDetailSkeleton"
import "./stocks.css"

export function StocksPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const { messages } = useLocale()
  const copy = messages.stocks.ui
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [analysis, setAnalysis] = useState<StockAnalysisResult | null>(null)
  const [parameters, setParameters] = useState<StockModelParameterSet | null>(null)
  const [loading, setLoading] = useState(true)
  const [detailLoading, setDetailLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [detailError, setDetailError] = useState<string | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState<StockDataSyncRunResult | null>(null)
  const stocksRef = useRef<StockWatchlistItem[]>([])
  const detailStageRef = useRef<HTMLDivElement>(null)
  const [detailMinHeight, setDetailMinHeight] = useState<number | null>(null)

  const applyStocks = useCallback((list: StockWatchlistItem[]) => {
    stocksRef.current = list
    setStocks(list)
    setSelectedKey((current) => current && list.some((stock) => stockKey(stock) === current) ? current : list[0] ? stockKey(list[0]) : null)
  }, [])

  const loadStocks = useCallback(async ({ preserveView = false }: { preserveView?: boolean } = {}) => {
    if (!preserveView) setLoading(true)
    setError(null)
    try {
      const list = await getStocks()
      applyStocks(list)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, copy.states.readError))
    } finally {
      if (!preserveView) setLoading(false)
    }
  }, [applyStocks, copy.states.readError])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void loadStocks() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [loadStocks])

  const loadDetail = useCallback(async (signal?: AbortSignal) => {
    const stock = stocksRef.current.find((item) => stockKey(item) === selectedKey)
    if (!stock) {
      setAnalysis(null)
      setParameters(null)
      return
    }

    setDetailLoading(true)
    setDetailMinHeight((current) => current ?? detailStageRef.current?.getBoundingClientRect().height ?? null)
    setDetailError(null)
    try {
      const [analysisResult, parameterResult] = await Promise.all([
        getStockAnalysis(stock.securityCode, stock.exchangeCode, signal),
        getModelParameters(stock.securityCode, stock.exchangeCode, signal),
      ])
      if (!signal?.aborted) {
        setAnalysis(analysisResult.analysis)
        setParameters(parameterResult)
      }
    } catch (detailLoadError) {
      if (!signal?.aborted) {
        setAnalysis(null)
        setParameters(null)
        setDetailError(getApiErrorMessage(detailLoadError, copy.states.analysisError))
      }
    } finally {
      if (!signal?.aborted) setDetailLoading(false)
    }
  }, [copy.states.analysisError, selectedKey])

  useEffect(() => {
    if (detailLoading || detailMinHeight === null) return

    const timeoutId = window.setTimeout(() => setDetailMinHeight(null), 420)
    return () => window.clearTimeout(timeoutId)
  }, [detailLoading, detailMinHeight])

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
      await loadStocks({ preserveView: true })
    } catch (syncError) {
      setError(getApiErrorMessage(syncError, copy.states.syncError))
    } finally {
      setSyncing(false)
    }
  }

  function selectStock(stock: StockWatchlistItem) {
    const nextKey = stockKey(stock)
    if (nextKey === selectedKey) return

    setDetailMinHeight((current) => current ?? detailStageRef.current?.getBoundingClientRect().height ?? null)
    setSelectedKey(nextKey)
    setAnalysis(null)
    setParameters(null)
    setDetailError(null)
    setDetailLoading(true)
  }

  if (loading) {
    return <PageFrame currentPath="/stocks" onNavigate={onNavigate} dataState="pending" contentClassName="stocks-page-wrap"><LoadingState label={copy.states.loadingList} /></PageFrame>
  }

  if (error && !stocks.length) {
    return <PageFrame currentPath="/stocks" onNavigate={onNavigate} dataState="unknown" contentClassName="stocks-page-wrap"><ErrorState message={error} onRetry={() => void loadStocks()} /></PageFrame>
  }

  if (!stocks.length) {
    return <PageFrame currentPath="/stocks" onNavigate={onNavigate} dataState="unknown" contentClassName="stocks-page-wrap"><EmptyState title={copy.states.emptyTitle} description={copy.states.emptyDescription} action={<Button onClick={() => onNavigate("/setup")}>{copy.actions.goSetup}</Button>} /></PageFrame>
  }

  const selectedStock = stocks.find((stock) => stockKey(stock) === selectedKey) ?? stocks[0]
  const analysisReady = hasAnalysisData(analysis)
  const lastUpdated = analysis?.computedAt ? formatDateTime(analysis.computedAt) : null
  const selectedRecommendation = analysis
    ? [{ analysis, suggestedBuyShares: 0, suggestedSellShares: 0, suggestedTradeAmount: 0, estimatedTransactionFeeAmount: 0 }]
    : []

  return (
    <PageFrame currentPath="/stocks" onNavigate={onNavigate} lastUpdated={lastUpdated} dataState={analysisReady ? "synced" : "pending"} contentClassName="stocks-page-wrap">
      <PageTitle
        eyebrow={copy.page.eyebrow}
        title={copy.page.title}
        description={copy.page.description}
        actions={<Button onClick={() => void syncAll()} disabled={syncing}>{syncing ? <><RefreshCw className="spin" data-icon="inline-start" />{copy.actions.syncing}</> : <><RefreshCw data-icon="inline-start" />{copy.actions.syncAll}</>}</Button>}
      />

      {error && <Alert variant="destructive" className="d-inline-alert"><AlertTitle>{copy.states.dataWarningTitle}</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}

      <div className="stocks-layout">
        <Card className="stock-directory">
          <CardHeader className="stock-directory-header">
            <SectionHeading label={copy.page.watchlistLabel} title={interpolate(copy.page.watchlistTitle, { count: stocks.length })} description={copy.page.watchlistDescription} />
          </CardHeader>
          <CardContent>
            <StockSelector
              stocks={stocks}
              recommendations={selectedRecommendation}
              selectedKey={selectedKey}
              onSelect={selectStock}
              labels={{
                kicker: copy.selector.kicker,
                title: copy.selector.title,
                description: interpolate(copy.selector.description, { count: stocks.length }),
                selectionHint: copy.selector.selectionHint,
                listLabel: copy.selector.listLabel,
                pending: copy.selector.pending,
                sectorUnset: copy.selector.sectorUnset,
                signals: copy.selector.signals,
              }}
            />
          </CardContent>
        </Card>

        <Card className="stock-detail">
          <CardContent className="stock-detail-content">
            <div ref={detailStageRef} className="stock-detail-stage" style={detailMinHeight ? { minHeight: detailMinHeight } : undefined}>
              <div className={`stock-detail-scene ${detailLoading ? "stock-detail-scene-loading" : "stock-detail-scene-ready"}`} key={`${selectedKey ?? "empty"}-${detailLoading ? "loading" : analysis?.computedAt ?? "pending"}`}>
                {detailLoading ? <StockDetailSkeleton label={copy.states.loadingAnalysis} /> : analysisReady && analysis ? <StockDetail analysis={analysis} parameters={parameters} onNavigate={onNavigate} /> : <StockDetailPending stock={selectedStock} message={detailError ?? (analysis ? copy.states.pendingMessage : null)} onSync={() => void syncAll()} />}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {syncResult && <section className="sync-results"><Alert variant={syncResult.partiallyFailedStockCount ? "attention" : "default"}><AlertTitle><Database className="sync-results-icon" />{copy.states.syncComplete}</AlertTitle><AlertDescription><span>{interpolate(copy.sync.attempted, { count: syncResult.attemptedStockCount, complete: syncResult.fullyCompletedStockCount, failed: syncResult.partiallyFailedStockCount })}</span>{syncResult.failures.length > 0 && <span> {interpolate(copy.sync.failures, { items: syncResult.failures.map((failure) => `${failure.securityCode} ${failure.dataKind}`).join(", ") })}</span>}</AlertDescription></Alert></section>}
    </PageFrame>
  )
}

function StockDetail({ analysis, parameters, onNavigate }: { analysis: StockAnalysisResult; parameters: StockModelParameterSet | null; onNavigate: (path: string) => void }) {
  const { locale, messages } = useLocale()
  const copy = messages.stocks.ui
  const decisionCopy = messages.dividendStrategy.ui.overview.decision
  const name = displayStockName(analysis)
  const identity = interpolate(copy.detail.dataIdentity, { code: analysis.securityCode, exchange: analysis.exchangeCode, exchangeLabel: exchangeLabel(analysis.exchangeCode), date: formatDate(analysis.dataAsOfDate) })

  return (
    <>
      <div className="stock-detail-header">
        <div>
          <div className="d-kicker"><span>01</span><span>{copy.detail.readingLabel}</span><i /></div>
          <div className="stock-detail-identity"><div className="d-stock-avatar">{name.slice(0, 2)}</div><div><h2 className="stock-detail-name">{name}</h2><p className="stock-detail-code">{identity}</p></div></div>
        </div>
        <Badge variant={analysis.priceZoneConfirmed ? "accent" : "attention"}>{analysis.priceZoneConfirmed ? copy.detail.confirmed : copy.detail.unconfirmed}</Badge>
      </div>

      <StockActionSummary analysis={analysis} labels={decisionCopy} />

      <div className="stock-analysis-grid">
        <AnalysisTile label={copy.detail.metrics.closePrice} value={formatMoney(analysis.closePrice)} />
        <AnalysisTile label={copy.detail.metrics.dividendYield} value={formatPercent(analysis.dividendYield)} />
        <AnalysisTile label={copy.detail.metrics.modelDividend} value={formatMoney(analysis.modelDividendPerShare)} />
        <AnalysisTile label={copy.detail.metrics.heldShares} value={analysis.heldShares.toLocaleString(locale)} />
        <AnalysisTile label={copy.detail.metrics.coreShares} value={analysis.coreShares.toLocaleString(locale)} />
        <AnalysisTile label={copy.detail.metrics.satelliteShares} value={analysis.satelliteShares.toLocaleString(locale)} />
      </div>

      <Separator />
      <section className="stock-detail-section">
        <div className="d-section-head">
          <div><div className="d-kicker"><span>02</span><span>{copy.detail.priceSectionLabel}</span><i /></div><h2>{copy.detail.priceSectionTitle}</h2><p>{copy.detail.priceSectionDescription}</p></div>
          <div className="d-current-price"><span>{copy.detail.currentPrice}</span><strong>{formatMoney(analysis.closePrice)}</strong></div>
        </div>
        <Card className="stock-ladder-card"><CardContent><PriceLadder analysis={analysis} /><PriceZoneBoard analysis={analysis} /></CardContent></Card>
      </section>

      <Separator />
      <section className="stock-detail-section">
        <SectionHeading label={copy.detail.parametersLabel} title={copy.detail.parametersTitle} description={copy.detail.parametersDescription} />
        {parameters ? <div className="parameter-grid">{parameterEntries(parameters, copy.detail.parameterNames).map(([label, value]) => <div className="parameter-item" key={label}><span>{label}</span><strong>{value}</strong></div>)}</div> : <EmptyState title={copy.detail.missingParametersTitle} description={copy.detail.missingParametersDescription} action={<Button variant="outline" onClick={() => onNavigate(`/settings?stock=${encodeURIComponent(`${analysis.securityCode}:${analysis.exchangeCode}`)}`)}>{copy.actions.configure}</Button>} />}
        <div className="note-box">{analysis.explanation || copy.states.explanationFallback}</div>
      </section>
    </>
  )
}

function AnalysisTile({ label, value }: { label: string; value: string }) {
  return <div className="analysis-tile"><span>{label}</span><strong>{value}</strong></div>
}

function StockDetailPending({ stock, message, onSync }: { stock: StockWatchlistItem; message: string | null; onSync: () => void }) {
  const { messages } = useLocale()
  const copy = messages.stocks.ui
  const name = displayStockName(stock)
  const identity = interpolate(copy.detail.pendingIdentity, { code: stock.securityCode, exchange: stock.exchangeCode })

  return (
    <div className="stock-detail-pending">
      <div className="d-kicker"><span>01</span><span>{copy.detail.readingLabel}</span><i /></div>
      <div className="stock-detail-identity"><div className="d-stock-avatar">{name.slice(0, 2)}</div><div><h2 className="stock-detail-name">{name}</h2><p className="stock-detail-code">{identity}</p></div></div>
      <h3>{copy.states.pendingTitle}</h3>
      <p>{copy.states.pendingDescription}</p>
      {message && <p className="stock-pending-message">{message}</p>}
      <Button variant="outline" onClick={onSync}><RefreshCw data-icon="inline-start" />{copy.actions.resync}</Button>
    </div>
  )
}

function parameterEntries(parameters: StockModelParameterSet, labels: Record<string, string>) {
  return [
    [labels.modelVersion, parameters.modelVersion],
    [labels.strongBuyYield, formatPercent(parameters.strongBuyYieldThreshold)],
    [labels.accumulationYield, formatPercent(parameters.accumulationYieldThreshold)],
    [labels.partialTrimYield, formatPercent(parameters.partialTrimYieldThreshold)],
    [labels.aggressiveTrimYield, formatPercent(parameters.aggressiveTrimYieldThreshold)],
    [labels.maxSecurityWeight, formatPercent(parameters.maxSecurityWeight)],
    [labels.maxSectorWeight, formatPercent(parameters.maxSectorWeight)],
    [labels.cashReserveRatio, formatPercent(parameters.cashReserveRatio)],
    [labels.maxSingleTradeAmount, formatMoney(parameters.maxSingleTradeAmount)],
    [labels.tradingLotSize, `${parameters.tradingLotSize} ${labels.tradingLotSuffix}`],
    [labels.effectiveFromDate, formatDate(parameters.effectiveFromDate)],
  ] as const
}
