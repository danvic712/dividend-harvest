import { ArrowDownRight, ArrowUpRight, Check, CircleDollarSign, RefreshCw, ShieldCheck } from "lucide-react"
import { useCallback, useEffect, useRef, useState } from "react"

import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { PageFrame } from "@/components/page-frame"
import { PageTitle, SectionHeading } from "@/components/page-heading"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card"
import { Field, FieldDescription, FieldLabel, FieldSet } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { getApiErrorMessage } from "@/lib/api-errors"
import { interpolate, useLocale } from "@/lib/i18n"
import { displayStockName } from "@/lib/stock-display"
import { formatMoney, formatNumber, stockKey, today } from "@/lib/utils"
import type { PortfolioTradeResult, RecordPortfolioTradeRequest, StockWatchlistItem } from "@/lib/api-types"
import { getPortfolioStocks, recordPortfolioTrade } from "@/features/portfolio/portfolio.api"
import "./portfolio.css"

type TradeDirection = "buy" | "sell"
type PortfolioPageProps = {
  onNavigate: (path: string) => void
  selectedStockKey: string
  onSelectedStockKeyChange: (value: string) => void
}

export function PortfolioPage({ onNavigate, selectedStockKey, onSelectedStockKeyChange }: PortfolioPageProps) {
  const { messages } = useLocale()
  const copy = messages.portfolio.ui
  const readErrorRef = useRef(copy.states.readError)
  const query = new URLSearchParams(window.location.search)
  const initialKey = query.get("stock") ?? (query.get("code") && query.get("exchange") ? `${query.get("code")}:${query.get("exchange")}` : null)
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [selectedKey, setSelectedKey] = useState(selectedStockKey || initialKey || "")
  const [direction, setDirection] = useState<TradeDirection>(query.get("direction") === "sell" ? "sell" : "buy")
  const [tradeDate, setTradeDate] = useState(today())
  const [quantity, setQuantity] = useState("")
  const [price, setPrice] = useState("")
  const [fee, setFee] = useState("0")
  const [sourceRecordId, setSourceRecordId] = useState("")
  const [result, setResult] = useState<PortfolioTradeResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    readErrorRef.current = copy.states.readError
  }, [copy.states.readError])

  useEffect(() => {
    if (selectedKey) onSelectedStockKeyChange(selectedKey)
  }, [onSelectedStockKeyChange, selectedKey])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const watchlist = await getPortfolioStocks()
      setStocks(watchlist)
      setSelectedKey((current) => current && watchlist.some((stock) => stockKey(stock) === current) ? current : watchlist[0] ? stockKey(watchlist[0]) : "")
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, readErrorRef.current))
    } finally {
      setLoading(false)
    }
  }, [setSelectedKey, setStocks])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])

  const selectedStock = stocks.find((stock) => stockKey(stock) === selectedKey)

  function changeDirection(nextDirection: TradeDirection) {
    setDirection(nextDirection)
    setResult(null)
  }

  function changeStock(nextKey: string | null) {
    if (!nextKey) return
    setSelectedKey(nextKey)
    onSelectedStockKeyChange(nextKey)
    window.sessionStorage.setItem("dividend-harvest-portfolio-stock", nextKey)
    setResult(null)
    const nextUrl = new URL(window.location.href)
    nextUrl.searchParams.set("stock", nextKey)
    window.history.replaceState({}, "", `${nextUrl.pathname}${nextUrl.search}${nextUrl.hash}`)
  }

  async function submit() {
    setError(null)
    setResult(null)
    const numericQuantity = Number(quantity)
    const numericPrice = Number(price)
    const numericFee = Number(fee)
    if (!selectedStock) {
      setError(copy.validation.stock)
      return
    }
    if (!quantity || !Number.isFinite(numericQuantity) || numericQuantity <= 0) {
      setError(copy.validation.quantity)
      return
    }
    if (!price || !Number.isFinite(numericPrice) || numericPrice <= 0) {
      setError(copy.validation.price)
      return
    }
    if (!Number.isFinite(numericFee) || numericFee < 0) {
      setError(copy.validation.fee)
      return
    }

    const request: RecordPortfolioTradeRequest = {
      securityCode: selectedStock.securityCode,
      exchangeCode: selectedStock.exchangeCode,
      tradeDate,
      tradeDirectionCode: direction,
      shareQuantity: numericQuantity,
      pricePerShare: numericPrice,
      transactionFeeAmount: numericFee,
      sourceRecordId: sourceRecordId.trim() || null,
    }

    setSubmitting(true)
    try {
      setResult(await recordPortfolioTrade(request))
      setQuantity("")
      setPrice("")
      setFee("0")
      setSourceRecordId("")
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, copy.states.recordError))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <PageFrame currentPath="/portfolio" onNavigate={onNavigate} dataState="pending" contentClassName="portfolio-page-wrap"><LoadingState label={copy.states.loading} /></PageFrame>
  }

  if (error && !stocks.length) {
    return <PageFrame currentPath="/portfolio" onNavigate={onNavigate} dataState="unknown" contentClassName="portfolio-page-wrap"><ErrorState message={error} onRetry={() => void load()} /></PageFrame>
  }

  if (!stocks.length) {
    return <PageFrame currentPath="/portfolio" onNavigate={onNavigate} dataState="unknown" contentClassName="portfolio-page-wrap"><EmptyState title={copy.states.emptyTitle} description={copy.states.emptyDescription} action={<Button onClick={() => onNavigate("/setup")}>{copy.actions.goSetup}</Button>} /></PageFrame>
  }

  const stockLabel = selectedStock ? `${displayStockName(selectedStock)} · ${selectedStock.securityCode}` : copy.result.stockFallback
  const stockName = selectedStock ? displayStockName(selectedStock) : copy.result.stockFallback
  const holding = selectedStock?.holding
  const parsedQuantity = Number(quantity)
  const parsedPrice = Number(price)
  const parsedFee = Number(fee)
  const hasValidFeePreview = fee === "" || (Number.isFinite(parsedFee) && parsedFee >= 0)
  const hasTradePreview = Boolean(quantity && Number.isFinite(parsedQuantity) && parsedQuantity > 0 && price && Number.isFinite(parsedPrice) && parsedPrice > 0 && hasValidFeePreview)
  const previewTradeAmount = hasTradePreview ? parsedQuantity * parsedPrice + (Number.isFinite(parsedFee) ? parsedFee : 0) : null
  const previewHeldShares = holding && hasTradePreview ? holding.heldShares + (direction === "buy" ? parsedQuantity : -parsedQuantity) : null

  function targetGapLabel(heldShares: number, targetShares: number) {
    const gap = targetShares - heldShares
    if (gap === 0) return copy.result.targetReached
    return gap > 0
      ? interpolate(copy.result.targetGapRemaining, { shares: formatNumber(gap, 0) })
      : interpolate(copy.result.targetGapExceeded, { shares: formatNumber(Math.abs(gap), 0) })
  }

  return (
    <PageFrame currentPath="/portfolio" onNavigate={onNavigate} dataState="synced" contentClassName="portfolio-page-wrap">
      <PageTitle
        eyebrow={copy.page.eyebrow}
        title={copy.page.title}
        description={copy.page.description}
        actions={<Button variant="outline" onClick={() => void load()} disabled={loading}><RefreshCw data-icon="inline-start" className={loading ? "spin" : undefined} />{loading ? copy.actions.refreshing : copy.actions.refresh}</Button>}
      />

      {error && <Alert variant="destructive" className="portfolio-feedback"><AlertTitle>{copy.states.noticeTitle}</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}

      <Card className="portfolio-guide-card">
        <CardHeader className="portfolio-guide-header">
          <SectionHeading label={copy.guide.eyebrow} title={copy.guide.title} description={copy.guide.description} />
          <span className="portfolio-guide-sticker" aria-hidden="true">↗</span>
        </CardHeader>
        <CardContent className="portfolio-guide-steps">
          {copy.guide.steps.map((step) => <div className="portfolio-guide-step" key={step.number}><span>{step.number}</span><div><strong>{step.title}</strong><p>{step.description}</p></div></div>)}
        </CardContent>
      </Card>

      <div className="portfolio-layout">
        <Card className="portfolio-form-card">
          <CardHeader className="portfolio-card-header">
            <SectionHeading label={copy.form.eyebrow} title={copy.form.title} description={copy.form.description} />
            <span className="portfolio-step-mark" aria-hidden="true">01</span>
          </CardHeader>
          <CardContent>
            {selectedStock && <div className="portfolio-stock-context">
              <span className="portfolio-stock-avatar" aria-hidden="true">{stockName.slice(0, 2)}</span>
              <div className="portfolio-stock-context-copy">
                <span>{copy.form.currentStock}</span>
                <strong>{stockName}</strong>
                <small>{selectedStock.securityCode} · {selectedStock.exchangeCode}</small>
              </div>
              <div className="portfolio-stock-position">
                <span>{copy.form.position}</span>
                <strong>{holding ? `${formatNumber(holding.heldShares, 0)} ${copy.result.shareUnit}` : copy.form.positionNone}</strong>
                <small>{holding ? interpolate(copy.form.positionHint, { cost: formatMoney(holding.averageCostPerShare) }) : copy.form.positionEmptyHint}</small>
              </div>
            </div>}
            <form className="portfolio-form" onSubmit={(event) => { event.preventDefault(); void submit() }}>
              <FieldSet className="portfolio-form-grid">
                <Field className="portfolio-grid-wide">
                  <FieldLabel id="trade-direction-label">{copy.form.direction}</FieldLabel>
                  <div className="portfolio-direction" role="group" aria-labelledby="trade-direction-label">
                    <Button type="button" variant="outline" className={`portfolio-direction-button ${direction === "buy" ? "portfolio-direction-active portfolio-direction-buy" : ""}`} onClick={() => changeDirection("buy")} aria-pressed={direction === "buy"}><ArrowUpRight size={16} aria-hidden="true" />{copy.form.buy}</Button>
                    <Button type="button" variant="outline" className={`portfolio-direction-button ${direction === "sell" ? "portfolio-direction-active portfolio-direction-sell" : ""}`} onClick={() => changeDirection("sell")} aria-pressed={direction === "sell"}><ArrowDownRight size={16} aria-hidden="true" />{copy.form.sell}</Button>
                  </div>
                </Field>

                <Field className="portfolio-grid-wide">
                  <FieldLabel htmlFor="trade-stock">{copy.form.stock}</FieldLabel>
                  <Select value={selectedKey} onValueChange={changeStock}>
                    <SelectTrigger id="trade-stock" className="portfolio-select-trigger"><SelectValue placeholder={copy.form.stockPlaceholder}>{selectedStock ? `${displayStockName(selectedStock)} · ${selectedStock.securityCode}` : copy.form.stockPlaceholder}</SelectValue></SelectTrigger>
                    <SelectContent><SelectGroup>{stocks.map((stock) => <SelectItem key={stockKey(stock)} value={stockKey(stock)}>{stock.securityCode} · {stock.exchangeCode} · {displayStockName(stock)}</SelectItem>)}</SelectGroup></SelectContent>
                  </Select>
                  <FieldDescription>{copy.form.stockHint}</FieldDescription>
                </Field>

                <Field>
                  <FieldLabel htmlFor="trade-date">{copy.form.date}</FieldLabel>
                  <Input id="trade-date" type="date" value={tradeDate} onChange={(event) => setTradeDate(event.target.value)} />
                </Field>
                <Field>
                  <FieldLabel htmlFor="trade-quantity">{copy.form.quantity}</FieldLabel>
                  <Input id="trade-quantity" type="number" min="1" step="1" value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder={copy.form.quantityPlaceholder} inputMode="numeric" />
                </Field>
                <Field>
                  <FieldLabel htmlFor="trade-price">{copy.form.price}</FieldLabel>
                  <Input id="trade-price" type="number" min="0.01" step="0.01" value={price} onChange={(event) => setPrice(event.target.value)} placeholder={copy.form.pricePlaceholder} inputMode="decimal" />
                </Field>
                <Field>
                  <FieldLabel htmlFor="trade-fee">{copy.form.fee}</FieldLabel>
                  <Input id="trade-fee" type="number" min="0" step="0.01" value={fee} onChange={(event) => setFee(event.target.value)} inputMode="decimal" />
                </Field>
                <Field className="portfolio-grid-wide">
                  <FieldLabel htmlFor="trade-source">{copy.form.source}</FieldLabel>
                  <Input id="trade-source" value={sourceRecordId} onChange={(event) => setSourceRecordId(event.target.value)} placeholder={copy.form.sourcePlaceholder} />
                  <FieldDescription>{copy.form.sourceHint}</FieldDescription>
                </Field>
              </FieldSet>

              <CardFooter className="portfolio-form-footer">
                <div className="portfolio-submit-hint"><Check size={15} aria-hidden="true" /><span>{stockLabel} · {direction === "buy" ? copy.form.buy : copy.form.sell}</span></div>
                <Button size="lg" type="submit" disabled={submitting}>{submitting ? copy.actions.submitting : copy.actions.submit}<ArrowUpRight data-icon="inline-end" /></Button>
              </CardFooter>
            </form>
          </CardContent>
        </Card>

        <Card className="portfolio-result-card">
          <CardHeader className="portfolio-card-header">
            <SectionHeading label={copy.result.eyebrow} title={copy.result.title} description={interpolate(copy.result.description, { stock: stockLabel })} />
            <CircleDollarSign className="portfolio-result-icon" size={20} aria-hidden="true" />
          </CardHeader>
          <CardContent>
            {result ? (
              <div className="portfolio-result-content">
                <div className="portfolio-result-hero">
                  <span>{result.tradeDirectionCode === "buy" ? copy.result.buyResult : copy.result.sellResult}</span>
                  <strong>{formatMoney(result.tradePrincipalAmount)}</strong>
                  <small>{copy.result.principal}</small>
                </div>
                <div className="portfolio-result-list">
                  <div><span>{copy.result.heldShares}</span><strong>{formatNumber(result.heldShares, 0)} {copy.result.shareUnit}</strong></div>
                  <div><span>{copy.result.coreShares}</span><strong>{formatNumber(result.coreShares, 0)} {copy.result.shareUnit}</strong></div>
                  <div><span>{copy.result.targetShares}</span><strong>{formatNumber(result.targetShares, 0)} {copy.result.shareUnit}</strong></div>
                  <div><span>{copy.result.targetGap}</span><strong>{targetGapLabel(result.heldShares, result.targetShares)}</strong></div>
                </div>
                <div className="portfolio-result-cost"><span>{copy.result.averageCost}</span><strong>{formatMoney(result.averageCostPerShare)}</strong><small>{copy.result.averageCostHint}</small></div>
                <Alert className="portfolio-result-alert"><ShieldCheck size={17} aria-hidden="true" /><div><AlertTitle>{copy.result.savedTitle}</AlertTitle><AlertDescription>{copy.result.savedDescription}</AlertDescription></div></Alert>
              </div>
            ) : (
              <div className="portfolio-result-content">
                <div className="portfolio-holding-preview">
                  <span>{copy.result.currentHolding}</span>
                  <strong>{holding ? `${formatNumber(holding.heldShares, 0)} ${copy.result.shareUnit}` : copy.result.noHolding}</strong>
                  <small>{holding ? interpolate(copy.result.currentHoldingHint, { cost: formatMoney(holding.averageCostPerShare) }) : copy.result.placeholderDescription}</small>
                </div>
                <div className="portfolio-result-list portfolio-result-list-preview">
                  <div><span>{copy.result.coreShares}</span><strong>{holding ? `${formatNumber(holding.coreShares, 0)} ${copy.result.shareUnit}` : copy.result.placeholderValue}</strong></div>
                  <div><span>{copy.result.targetShares}</span><strong>{holding ? `${formatNumber(holding.targetShares, 0)} ${copy.result.shareUnit}` : copy.result.placeholderValue}</strong></div>
                  <div><span>{copy.result.targetGap}</span><strong>{holding ? targetGapLabel(holding.heldShares, holding.targetShares) : copy.result.placeholderValue}</strong></div>
                </div>
                {holding && <div className="portfolio-result-cost"><span>{copy.result.averageCost}</span><strong>{formatMoney(holding.averageCostPerShare)}</strong><small>{copy.result.averageCostHint}</small></div>}
                {hasTradePreview ? (
                  <div className="portfolio-trade-preview">
                    <div><span>{copy.result.previewTradeAmount}</span><strong>{formatMoney(previewTradeAmount)}</strong></div>
                    <div><span>{copy.result.previewHeldShares}</span><strong>{previewHeldShares === null ? copy.result.placeholderValue : `${formatNumber(previewHeldShares, 0)} ${copy.result.shareUnit}`}</strong></div>
                    <p>{copy.result.previewHint}</p>
                  </div>
                ) : (
                  <div className="portfolio-result-note"><ShieldCheck size={16} aria-hidden="true" /><div><strong>{holding ? copy.result.emptyInputTitle : copy.result.placeholderTitle}</strong><p>{holding ? copy.result.emptyInputDescription : copy.result.placeholderDescription}</p></div></div>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </PageFrame>
  )
}
