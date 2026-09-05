import { ArrowUpRight, Check, CircleDollarSign, RefreshCw, Sparkles } from "lucide-react"
import { useCallback, useEffect, useState } from "react"

import { ErrorState, LoadingState } from "@/components/async-state"
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
import { formatDateTime, formatMoney, stockKey, today } from "@/lib/utils"
import type { BudgetSummary, RecordCashLedgerEntryRequest, StockWatchlistItem } from "@/lib/api-types"
import { getBudget, recordBudgetEntry } from "@/features/budget/budget.api"
import { getStocks } from "@/features/stocks/stocks.api"
import "./budget.css"

type EntryType = "budget_deposit" | "dividend_received" | "buy" | "sell" | "fee" | "cash_adjustment"

const entryTypeDefinitions: Array<{ code: EntryType; direction: "inflow" | "outflow"; needsStock: boolean }> = [
  { code: "budget_deposit", direction: "inflow", needsStock: false },
  { code: "dividend_received", direction: "inflow", needsStock: true },
  { code: "buy", direction: "outflow", needsStock: true },
  { code: "sell", direction: "inflow", needsStock: true },
  { code: "fee", direction: "outflow", needsStock: false },
  { code: "cash_adjustment", direction: "inflow", needsStock: false },
]

export function BudgetPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const { messages } = useLocale()
  const copy = messages.budget.ui
  const [summary, setSummary] = useState<BudgetSummary | null>(null)
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [entryType, setEntryType] = useState<EntryType>("budget_deposit")
  const [entryDate, setEntryDate] = useState(today())
  const [amount, setAmount] = useState("")
  const [selectedStock, setSelectedStock] = useState("")
  const [sourceRecordId, setSourceRecordId] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [budget, watchlist] = await Promise.all([getBudget(), getStocks()])
      setSummary(budget)
      setStocks(watchlist)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, copy.states.readError))
    } finally {
      setLoading(false)
    }
  }, [copy.states.readError])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])

  const selectedType = entryTypeDefinitions.find((item) => item.code === entryType) ?? entryTypeDefinitions[0]
  const selectedTypeLabel = copy.entryTypes[selectedType.code]
  const selectedStockItem = stocks.find((stock) => stockKey(stock) === selectedStock)

  function changeEntryType(value: string | null) {
    if (!value) return
    setEntryType(value as EntryType)
    const nextType = entryTypeDefinitions.find((item) => item.code === value)
    if (!nextType?.needsStock) setSelectedStock("")
  }

  async function submit() {
    setError(null)
    setMessage(null)
    const numericAmount = Number(amount)
    if (!amount || !Number.isFinite(numericAmount) || numericAmount <= 0) {
      setError(copy.validation.amount)
      return
    }
    if (selectedType.needsStock && !selectedStock) {
      setError(copy.validation.stock)
      return
    }

    const stock = stocks.find((item) => stockKey(item) === selectedStock)
    const request: RecordCashLedgerEntryRequest = {
      entryDate,
      entryTypeCode: entryType,
      cashDirectionCode: selectedType.direction,
      cashAmount: numericAmount,
      securityCode: stock?.securityCode ?? null,
      exchangeCode: stock?.exchangeCode ?? null,
      sourceRecordId: sourceRecordId.trim() || null,
    }

    setSubmitting(true)
    try {
      await recordBudgetEntry(request)
      setAmount("")
      setSourceRecordId("")
      setMessage(copy.states.successMessage)
      await load()
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, copy.states.recordError))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <PageFrame currentPath="/budget" onNavigate={onNavigate} dataState="pending" contentClassName="budget-page-wrap"><LoadingState label={copy.states.loading} /></PageFrame>
  }

  if (error && !summary) {
    return <PageFrame currentPath="/budget" onNavigate={onNavigate} dataState="unknown" contentClassName="budget-page-wrap"><ErrorState message={error} onRetry={() => void load()} /></PageFrame>
  }

  if (!summary) {
    return <PageFrame currentPath="/budget" onNavigate={onNavigate} dataState="unknown" contentClassName="budget-page-wrap"><ErrorState message={copy.states.noSummary} onRetry={() => void load()} /></PageFrame>
  }

  return (
    <PageFrame currentPath="/budget" onNavigate={onNavigate} lastUpdated={formatDateTime(summary.computedAt)} dataState="synced" contentClassName="budget-page-wrap">
      <PageTitle
        eyebrow={copy.page.eyebrow}
        title={copy.page.title}
        description={copy.page.description}
        actions={<Button variant="outline" onClick={() => void load()} disabled={loading}><RefreshCw data-icon="inline-start" className={loading ? "spin" : undefined} />{loading ? copy.actions.refreshing : copy.actions.refresh}</Button>}
      />

      {error && <Alert variant="destructive" className="budget-feedback"><AlertTitle>{copy.states.noticeTitle}</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      {message && <Alert className="budget-feedback"><AlertTitle>{copy.states.successTitle}</AlertTitle><AlertDescription>{message}</AlertDescription></Alert>}

      <Card className="budget-guide-card">
        <CardHeader className="budget-guide-header">
          <SectionHeading label={copy.guide.eyebrow} title={copy.guide.title} description={copy.guide.description} />
          <span className="budget-guide-sticker" aria-hidden="true">↗</span>
        </CardHeader>
        <CardContent className="budget-guide-steps">
          {copy.guide.steps.map((step) => <div className="budget-guide-step" key={step.number}><span>{step.number}</span><div><strong>{step.title}</strong><p>{step.description}</p></div></div>)}
        </CardContent>
      </Card>

      <div className="budget-layout">
        <Card className="budget-form-card">
          <CardHeader className="budget-card-header">
            <SectionHeading label={copy.form.eyebrow} title={copy.form.title} description={copy.form.description} />
            <span className="budget-step-mark" aria-hidden="true">01</span>
          </CardHeader>
          <CardContent>
            <form className="budget-form" onSubmit={(event) => { event.preventDefault(); void submit() }}>
              <FieldSet className="budget-form-grid">
                <Field>
                  <FieldLabel htmlFor="entry-date">{copy.form.date}</FieldLabel>
                  <Input id="entry-date" type="date" value={entryDate} onChange={(event) => setEntryDate(event.target.value)} />
                </Field>

                <Field>
                  <FieldLabel htmlFor="entry-type">{copy.form.type}</FieldLabel>
                  <Select value={entryType} onValueChange={changeEntryType}>
                    <SelectTrigger id="entry-type" className="budget-select-trigger"><SelectValue>{selectedTypeLabel}</SelectValue></SelectTrigger>
                    <SelectContent><SelectGroup>{entryTypeDefinitions.map((item) => <SelectItem key={item.code} value={item.code}>{copy.entryTypes[item.code]}</SelectItem>)}</SelectGroup></SelectContent>
                  </Select>
                </Field>

                <Field>
                  <FieldLabel htmlFor="entry-amount">{copy.form.amount}</FieldLabel>
                  <Input id="entry-amount" type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder={copy.form.amountPlaceholder} inputMode="decimal" />
                </Field>

                <Field>
                  <FieldLabel htmlFor="entry-direction">{copy.form.direction}</FieldLabel>
                  <Input id="entry-direction" value={selectedType.direction === "inflow" ? copy.form.inflow : copy.form.outflow} readOnly aria-readonly="true" />
                </Field>

                {selectedType.needsStock && (
                  <Field className="budget-grid-wide">
                    <FieldLabel htmlFor="entry-stock">{copy.form.stock}</FieldLabel>
                    <Select value={selectedStock || "__none__"} onValueChange={(value) => setSelectedStock(value === "__none__" || !value ? "" : value)}>
                      <SelectTrigger id="entry-stock" className="budget-select-trigger"><SelectValue placeholder={copy.form.stockPlaceholder}>{selectedStockItem ? `${displayStockName(selectedStockItem)} · ${selectedStockItem.securityCode}` : copy.form.stockPlaceholder}</SelectValue></SelectTrigger>
                      <SelectContent><SelectGroup><SelectItem value="__none__">{copy.form.stockPlaceholder}</SelectItem>{stocks.map((stock) => <SelectItem key={stockKey(stock)} value={stockKey(stock)}>{stock.securityCode} · {stock.exchangeCode} · {displayStockName(stock)}</SelectItem>)}</SelectGroup></SelectContent>
                    </Select>
                    <FieldDescription>{stocks.length ? copy.form.stockHint : copy.states.emptyStock}</FieldDescription>
                  </Field>
                )}

                <Field className="budget-grid-wide">
                  <FieldLabel htmlFor="source-record">{copy.form.source}</FieldLabel>
                  <Input id="source-record" value={sourceRecordId} onChange={(event) => setSourceRecordId(event.target.value)} placeholder={copy.form.sourcePlaceholder} />
                  <FieldDescription>{copy.form.sourceHint}</FieldDescription>
                </Field>
              </FieldSet>

              <CardFooter className="budget-form-footer">
                <div className="budget-submit-hint"><Check size={15} aria-hidden="true" /><span>{selectedTypeLabel} · {selectedType.direction === "inflow" ? copy.form.inflow : copy.form.outflow}</span></div>
                <Button size="lg" type="submit" disabled={submitting}>{submitting ? copy.actions.submitting : copy.actions.submit}<ArrowUpRight data-icon="inline-end" /></Button>
              </CardFooter>
            </form>
          </CardContent>
        </Card>

        <Card className="budget-summary-card">
          <CardHeader className="budget-card-header">
            <SectionHeading label={copy.summary.eyebrow} title={copy.summary.title} description={interpolate(copy.summary.description, { count: summary.entryCount })} />
            <CircleDollarSign className="budget-summary-icon" size={20} aria-hidden="true" />
          </CardHeader>
          <CardContent>
            <div className="cash-balance">
              <span>{copy.summary.balance}</span>
              <strong>{formatMoney(summary.cashBalanceAmount)}</strong>
              <small>{formatDateTime(summary.computedAt)}</small>
            </div>

            <div className="budget-mini-grid">
              <div className="budget-mini budget-mini-inflow"><span>{copy.summary.inflow}</span><strong>{formatMoney(summary.totalInflowAmount)}</strong></div>
              <div className="budget-mini budget-mini-outflow"><span>{copy.summary.outflow}</span><strong>{formatMoney(summary.totalOutflowAmount)}</strong></div>
            </div>

            <div className="budget-note"><Sparkles size={16} aria-hidden="true" /><p>{copy.summary.note}</p></div>
            <Button variant="link" className="budget-portfolio-link" onClick={() => onNavigate("/portfolio")}>{copy.summary.portfolioHint}<ArrowUpRight data-icon="inline-end" /></Button>
          </CardContent>
        </Card>
      </div>
    </PageFrame>
  )
}
