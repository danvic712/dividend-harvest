import { AlertCircle, ChevronRight, Clock3, Database, RefreshCw, Save, ShieldCheck } from "lucide-react"
import { useCallback, useEffect, useMemo, useState } from "react"

import { PageFrame } from "@/components/page-frame"
import { SectionHeading } from "@/components/page-heading"
import { EmptyState, ErrorState } from "@/components/async-state"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { StockSelector } from "@/components/stock-selector"
import { getApiErrorMessage } from "@/lib/api-errors"
import { interpolate, useLocale, type OverviewCopy } from "@/lib/i18n"
import { currentPriceZoneLabel } from "@/lib/price-zones"
import { analysisDisplayName, displayStockName, hasAnalysisData, recommendationLabel, recommendationTone } from "@/lib/stock-display"
import { formatDateTime, formatMoney, formatNumber, stockKey } from "@/lib/utils"
import type { BudgetSummary, PortfolioRecommendationResult, StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"
import { createRecommendationSnapshot, getBudgetSummary, getRecommendations, getWatchlist } from "@/features/recommendations/recommendations.api"
import { RecommendationDecision } from "@/features/recommendations/RecommendationDecision"
import { RecommendationsSkeleton } from "@/features/recommendations/RecommendationsSkeleton"
import "./recommendations.css"

type RecommendationsPageProps = {
  onNavigate: (path: string) => void
  notice?: string | null
}

export function RecommendationsPage({ onNavigate, notice }: RecommendationsPageProps) {
  const { messages } = useLocale()
  const copy = messages.dividendStrategy.ui.overview
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
    const [watchlistResult, recommendationResult, budgetResult] = await Promise.allSettled([getWatchlist(), getRecommendations(), getBudgetSummary()])

    if (watchlistResult.status === "rejected") {
      setError(getApiErrorMessage(watchlistResult.reason, copy.messages.readUnavailable))
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
    }

    if (budgetResult.status === "fulfilled") {
      setBudget(budgetResult.value)
    } else {
      setBudget(null)
    }

    setLoading(false)
  }, [copy.messages.readUnavailable])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])

  const selectedRecommendation = useMemo<StockRecommendationResult | null>(() => recommendation?.stocks.find((item) => stockKey(item.analysis) === selectedKey) ?? null, [recommendation, selectedKey])
  const hasAnyReadyRecommendation = Boolean(recommendation?.stocks.some((item) => hasAnalysisData(item.analysis)))
  const hasCompleteRecommendation = Boolean(recommendation?.stocks.length && recommendation.stocks.every((item) => hasAnalysisData(item.analysis)))
  const readyRecommendation = hasAnyReadyRecommendation ? recommendation : null
  const selectedRecommendationReady = hasAnalysisData(selectedRecommendation?.analysis)
  const selectedStock = stocks.find((stock) => stockKey(stock) === selectedKey)
  const lastUpdated = recommendation?.computedAt ?? budget?.computedAt

  async function saveSnapshot() {
    setSavingSnapshot(true)
    setSnapshotMessage(null)
    try {
      await createRecommendationSnapshot()
      setSnapshotMessage(copy.messages.saveSuccess)
    } catch (snapshotError) {
      setSnapshotMessage(getApiErrorMessage(snapshotError, copy.messages.saveFailed))
    } finally {
      setSavingSnapshot(false)
    }
  }

  if (loading) return <PageFrame currentPath="/overview" onNavigate={onNavigate} dataState="pending"><div className="overview-page"><RecommendationsSkeleton label={copy.messages.loadingOverview} /></div></PageFrame>
  if (error && !stocks.length) return <PageFrame currentPath="/overview" onNavigate={onNavigate} dataState="pending"><div className="overview-page overview-state-page"><ErrorState message={error} onRetry={() => void load()} /></div></PageFrame>
  if (!stocks.length) return <PageFrame currentPath="/overview" onNavigate={onNavigate} dataState="unknown"><div className="overview-page overview-state-page"><EmptyState title={copy.messages.emptyTitle} description={copy.messages.emptyDescription} action={<Button onClick={() => onNavigate("/setup")}>{copy.actions.setupPortfolio}</Button>} /></div></PageFrame>

  return (
    <PageFrame currentPath="/overview" onNavigate={onNavigate} lastUpdated={lastUpdated ? formatDateTime(lastUpdated) : null} dataState={hasCompleteRecommendation ? "synced" : "pending"}>
      <div className="overview-page">
        <div className="d-breadcrumb"><span>{copy.breadcrumb.myStocks}</span><ChevronRight size={13} /><strong>{selectedStock ? displayStockName(selectedStock) : copy.breadcrumb.todayDecision}</strong><span>{selectedStock ? `${selectedStock.securityCode}.${selectedStock.exchangeCode} · ${copy.breadcrumb.aShareReference}` : copy.breadcrumb.aShareReference}</span></div>
        {notice && <Alert variant="attention" className="d-inline-alert"><AlertTitle>{copy.messages.noticeTitle}</AlertTitle><AlertDescription>{notice}</AlertDescription></Alert>}
        {error && <Alert variant="destructive" className="d-inline-alert"><AlertTitle>{copy.messages.errorTitle}</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
        <DailyNotebookHeader
          dataState={hasCompleteRecommendation ? "synced" : "pending"}
          labels={copy}
          lastUpdated={lastUpdated ? formatDateTime(lastUpdated) : null}
          actions={hasCompleteRecommendation ? <>
            <Button variant="outline" size="sm" className="d-notebook-tool" onClick={() => void load()}><RefreshCw data-icon="inline-start" />{copy.actions.refresh}</Button>
            <Button variant="secondary" size="sm" className="d-notebook-tool" onClick={() => void saveSnapshot()} disabled={savingSnapshot}><Save data-icon="inline-start" />{savingSnapshot ? copy.actions.savingSnapshot : copy.actions.saveSnapshot}</Button>
          </> : undefined}
        />
        <StockSelector stocks={stocks} recommendations={recommendation?.stocks ?? []} selectedKey={selectedKey} onSelect={(stock) => setSelectedKey(stockKey(stock))} labels={{ kicker: copy.stockSelector.kicker, title: copy.stockSelector.title, description: interpolate(copy.stockSelector.description, { count: stocks.length }), selectionHint: copy.stockSelector.selectionHint, pending: copy.stockSelector.pending, listLabel: copy.stockSelector.listLabel, sectorUnset: copy.stockSelector.sectorUnset, signals: copy.stockSelector.signals }} />
        {readyRecommendation ? <>
          <ReadyPortfolioPulse recommendation={readyRecommendation} budget={budget} labels={copy} />
          {snapshotMessage && <p className="form-message d-inline-message">{snapshotMessage}</p>}
          {selectedRecommendation && selectedRecommendationReady ? <>
            <ReadyGuidance recommendation={selectedRecommendation} labels={copy} />
            <RecommendationDecision recommendation={selectedRecommendation} onRecord={(direction) => onNavigate(`/portfolio?stock=${encodeURIComponent(stockKey(selectedRecommendation.analysis))}&direction=${direction}`)} />
          </> : selectedStock ? <PendingDecisionView selectedStock={selectedStock} message={copy.messages.recommendationPending} labels={copy} onRefresh={() => void load()} onNavigate={onNavigate} budget={budget} /> : <EmptyState title={copy.messages.emptyStockTitle} description={copy.messages.emptyStockDescription} action={<Button onClick={() => onNavigate("/stocks")}>{copy.actions.viewStockData}</Button>} />}
          <section className="analysis-section"><SectionHeading label={copy.ready.compositionLabel} title={copy.ready.compositionTitle} description={copy.ready.compositionDescription} /><div className="decision-support"><div className="support-tile"><span>{copy.ready.startingBudget}</span><strong>{formatMoney(readyRecommendation.startingAvailableBudgetAmount)}</strong></div><div className="support-tile"><span>{copy.ready.suggestedShares}</span><strong>{formatNumber(readyRecommendation.stocks.reduce((total, item) => total + item.suggestedBuyShares + item.suggestedSellShares, 0), 0)} {copy.ready.sharesUnit}</strong></div><div className="support-tile"><span>{copy.ready.calculationTime}</span><strong>{formatDateTime(readyRecommendation.computedAt)}</strong></div></div></section>
        </> : <PendingDecisionView selectedStock={selectedStock} message={null} labels={copy} onRefresh={() => void load()} onNavigate={onNavigate} budget={budget} />}
      </div>
    </PageFrame>
  )
}

function DailyNotebookHeader({ dataState, labels, lastUpdated, actions }: { dataState: "synced" | "pending"; labels: OverviewCopy; lastUpdated: string | null; actions?: React.ReactNode }) {
  const statusTitle = dataState === "synced" ? labels.masthead.syncedTitle : labels.masthead.pendingTitle
  const statusDescription = lastUpdated ? interpolate(labels.masthead.updatedAt, { time: lastUpdated }) : labels.masthead.updatedHint

  return (
    <section className={`d-notebook-header d-notebook-header-${dataState}`} aria-labelledby="daily-notebook-title">
      <div className="d-notebook-index" aria-hidden="true"><span>D</span><small>NOTE</small></div>
      <div className="d-notebook-copy">
        <div className="d-kicker"><span>—</span><span>{labels.masthead.kicker}</span><i /></div>
        <h1 id="daily-notebook-title">{labels.masthead.title}</h1>
        <p>{labels.masthead.description}</p>
      </div>
      <div className="d-notebook-aside">
        <div className="d-notebook-status" role="status" aria-live="polite" aria-atomic="true">
          <span className="d-notebook-status-dot" />
          <div><strong>{statusTitle}</strong><small>{statusDescription}</small></div>
        </div>
        {actions && <div className="d-notebook-tools">{actions}</div>}
      </div>
    </section>
  )
}

function ReadyGuidance({ recommendation, labels }: { recommendation: StockRecommendationResult; labels: OverviewCopy }) {
  const { analysis } = recommendation
  const normalizedCode = analysis.recommendationCode.toLowerCase()
  const isSell = normalizedCode.includes("trim")
  const actionShares = isSell ? recommendation.suggestedSellShares : recommendation.suggestedBuyShares
  const kind = normalizedCode.includes("trim") ? "trim" : normalizedCode === "strong_buy" || normalizedCode === "accumulate" ? "buy" : normalizedCode === "re_evaluate" ? "review" : "hold"
  const guidance = {
    buy: { title: labels.guidance.buyTitle, description: labels.guidance.buyDescription },
    trim: { title: labels.guidance.trimTitle, description: labels.guidance.trimDescription },
    review: { title: labels.guidance.reviewTitle, description: labels.guidance.reviewDescription },
    hold: { title: labels.guidance.holdTitle, description: labels.guidance.holdDescription },
  }[kind]
  const amount = actionShares > 0 ? formatMoney(recommendation.suggestedTradeAmount) : labels.guidance.notApplicable
  const shares = actionShares > 0 ? `${formatNumber(actionShares, 0)} ${labels.ready.sharesUnit}` : labels.guidance.notApplicable
  const zone = currentPriceZoneLabel(analysis, labels.decision.priceZones)

  return (
    <section className={`surface d-guidance d-guidance-${kind}`} aria-labelledby="ready-guidance-title">
      <div className="d-guidance-heading">
        <div className="d-kicker"><span>01</span><span>{labels.guidance.kicker}</span><i /></div>
        <span>{labels.guidance.referenceNote}</span>
      </div>
      <div className="d-guidance-content">
        <div className="d-guidance-mark" aria-hidden="true">{kind === "buy" ? "＋" : kind === "trim" ? "−" : kind === "review" ? "?" : "·"}</div>
        <div className="d-guidance-copy"><h2 id="ready-guidance-title">{guidance.title}</h2><p>{interpolate(guidance.description, { amount, shares, zone })}</p></div>
        <div className="d-guidance-metrics">
          <div><span>{labels.guidance.amountLabel}</span><strong>{amount}</strong></div>
          <div><span>{labels.guidance.sharesLabel}</span><strong>{shares}</strong></div>
          <div><span>{labels.guidance.rangeLabel}</span><strong>{zone}</strong></div>
        </div>
      </div>
      <p className="d-guidance-foot">{labels.guidance.foot}</p>
    </section>
  )
}

function ReadyPortfolioPulse({ recommendation, budget, labels }: { recommendation: PortfolioRecommendationResult; budget: BudgetSummary | null; labels: OverviewCopy }) {
  return (
    <section className="surface d-portfolio-pulse" aria-labelledby="portfolio-pulse-title">
      <div className="d-pulse-top">
        <div className="d-pulse-intro">
          <p className="eyebrow">{labels.ready.portfolioPulse}</p>
          <strong id="portfolio-pulse-title" className="summary-number">{formatMoney(recommendation.totalSuggestedTradeAmount)}</strong>
          <p className="muted-copy">{interpolate(labels.ready.summary, { count: recommendation.stocks.length })}</p>
        </div>
        <div className="summary-side">
          <div className="summary-side-item"><span>{labels.ready.availableBudget}</span><strong>{formatMoney(recommendation.remainingAvailableBudgetAmount)}</strong></div>
          <div className="summary-side-item"><span>{labels.ready.estimatedFees}</span><strong>{formatMoney(recommendation.estimatedTransactionFeeAmount)}</strong></div>
          <div className="summary-side-item"><span>{labels.ready.cashBalance}</span><strong>{formatMoney(budget?.cashBalanceAmount)}</strong></div>
        </div>
      </div>
      <div className="d-pulse-stock-list" role="list" aria-label={labels.stockSelector.listLabel}>
        {recommendation.stocks.map((item) => {
          const ready = hasAnalysisData(item.analysis)
          const tone = ready ? recommendationTone(item.analysis.recommendationCode) : "pending"
          const isSell = item.analysis.recommendationCode.toLowerCase().includes("trim")
          const shares = isSell ? item.suggestedSellShares : item.suggestedBuyShares
          const status = ready ? recommendationLabel(item.analysis.recommendationCode, labels.stockSelector.signals) : labels.stockSelector.pending
          const action = ready
            ? shares > 0 ? `${isSell ? labels.decision.sell : labels.decision.buy} ${formatNumber(shares, 0)} ${labels.ready.sharesUnit}` : labels.decision.hold
            : labels.pending.syncLabel
          return <div key={stockKey(item.analysis)} className={`d-pulse-stock d-pulse-stock-${tone}`} role="listitem"><span className="d-pulse-stock-dot" /><div className="d-pulse-stock-copy"><strong>{analysisDisplayName(item.analysis)}</strong><span>{item.analysis.securityCode}.{item.analysis.exchangeCode}</span></div><div className="d-pulse-stock-action"><strong>{status}</strong><small>{action}</small></div></div>
        })}
      </div>
    </section>
  )
}

function PendingDecisionView({ selectedStock, message, labels, onRefresh, onNavigate, budget }: { selectedStock?: StockWatchlistItem; message: string | null; labels: OverviewCopy; onRefresh: () => void; onNavigate: (path: string) => void; budget: BudgetSummary | null }) {
  const stockName = selectedStock ? displayStockName(selectedStock) : labels.pending.stockFallback
  return <div className="d-pending-workspace">
    <div className="d-pending-workspace-head">
      <div className="d-kicker"><span>01</span><span>{labels.pending.actionKicker}</span><i /></div>
      <div className="d-workflow-progress" aria-label={labels.pending.workflowLabel}><span className="d-workflow-step-active">01</span><i /><span>02</span><i /><span>03</span></div>
      <div className="d-pending-context"><span className="d-live-dot" /><div><strong>{labels.pending.syncLabel}</strong><small>{labels.pending.syncHint}</small></div></div>
    </div>
    <section className="d-hero d-pending-hero">
      <div className="d-story"><div className="d-stock-identity"><div className="d-stock-avatar">{stockName.slice(0, 2)}</div><div><strong>{stockName}</strong><span>{selectedStock ? `${selectedStock.securityCode}.${selectedStock.exchangeCode} · ${labels.breadcrumb.aShareWatchlist}` : labels.pending.stockReference}</span></div></div><h2 className="d-story-title">{labels.pending.heroLead}<span>{labels.pending.heroAccent}</span></h2><p className="d-hero-copy">{labels.pending.heroDescription}</p><div className="d-action-row"><div className="d-action-amount"><div className="d-action-icon d-pending-icon"><RefreshCw size={17} className="spin" /></div><div><span>{labels.pending.statusLabel}</span><strong>{labels.pending.statusTitle}</strong><small>{message ?? labels.pending.statusHint}</small></div></div><Button className="d-primary-button" onClick={onRefresh}>{labels.actions.checkUpdates}<ArrowRightIcon /></Button></div></div>
      <section className="surface d-signal-card d-pending-card"><div className="d-card-topline"><span>02 / 03 · {labels.pending.nextChecks}</span><Database size={16} /></div><h2>{labels.pending.cardTitle}</h2><p className="d-card-description">{labels.pending.cardDescription}</p><div className="d-pending-status-list"><PendingStatus label={labels.pending.companyProfile} detail={labels.pending.companyProfileDetail} waitingLabel={labels.pending.waiting} /><PendingStatus label={labels.pending.trailingDividend} detail={labels.pending.trailingDividendDetail} waitingLabel={labels.pending.waiting} /><PendingStatus label={labels.pending.buyingRange} detail={labels.pending.buyingRangeDetail} waitingLabel={labels.pending.waiting} /></div><div className="d-data-foot"><Clock3 size={13} /> {labels.pending.dataFoot}</div></section>
    </section>
    <section className="d-pending-grid"><div className="surface d-pending-next"><div className="d-card-topline"><span>03 / 03 · {labels.pending.availableNow}</span><ShieldCheck size={16} /></div><h2>{labels.pending.nextTitle}</h2><p>{labels.pending.nextDescription}</p><div className="d-pending-route" aria-label={labels.pending.routeLabel}><PendingRouteStep number="01" title={labels.pending.routeHolding} detail={labels.pending.routeHoldingDetail} /><PendingRouteStep number="02" title={labels.pending.routeSync} detail={labels.pending.routeSyncDetail} /><PendingRouteStep number="03" title={labels.pending.routeDecision} detail={labels.pending.routeDecisionDetail} /></div><div className="d-pending-actions"><Button variant="default" className="d-pending-action-primary" onClick={() => onNavigate("/portfolio")}>{labels.actions.fillHolding}<ChevronRight data-icon="inline-end" /></Button><Button variant="outline" className="d-pending-action-secondary" onClick={() => onNavigate("/budget")}>{labels.actions.recordCash}<ChevronRight data-icon="inline-end" /></Button></div></div><div className="surface d-pending-budget"><span>{labels.pending.cashBalance}</span><strong>{formatMoney(budget?.cashBalanceAmount)}</strong><small>{labels.pending.cashHint}</small></div></section>
  </div>
}

function PendingStatus({ label, detail, waitingLabel }: { label: string; detail: string; waitingLabel: string }) {
  return <div className="d-pending-status"><span className="d-pending-status-icon"><AlertCircle size={14} /></span><div><strong>{label}</strong><small>{detail}</small></div><span className="d-pending-status-label">{waitingLabel}</span></div>
}

function PendingRouteStep({ number, title, detail }: { number: string; title: string; detail: string }) {
  return <div className="d-pending-route-step"><span>{number}</span><div><strong>{title}</strong><small>{detail}</small></div></div>
}

function ArrowRightIcon() {
  return <ChevronRight data-icon="inline-end" />
}
