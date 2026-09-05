import { ArrowRight, Plus, Trash2 } from "lucide-react"
import { useState } from "react"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Checkbox } from "@/components/ui/checkbox"
import { Field, FieldDescription, FieldGroup, FieldLabel } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Separator } from "@/components/ui/separator"
import { getApiErrorMessage } from "@/lib/api-errors"
import { initializeSetup } from "@/features/setup/setup.api"
import type { SetupResult, SetupStockRequest } from "@/lib/api-types"
import { interpolate, useLocale } from "@/lib/i18n"
import "./setup.css"

function newStock(): SetupStockRequest {
  return { securityCode: "", exchangeCode: "SSE", initialHolding: null }
}

export function SetupPage({ onComplete }: { onComplete: (result: SetupResult) => void }) {
  const { messages } = useLocale()
  const copy = messages.setup.ui
  const [portfolioName, setPortfolioName] = useState("")
  const [stocks, setStocks] = useState<SetupStockRequest[]>([newStock()])
  const [withHolding, setWithHolding] = useState<boolean[]>([false])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function updateStock(index: number, update: Partial<SetupStockRequest>) {
    setStocks((current) => current.map((stock, itemIndex) => itemIndex === index ? { ...stock, ...update } : stock))
  }

  function toggleHolding(index: number, checked: boolean) {
    setWithHolding((current) => current.map((value, itemIndex) => itemIndex === index ? checked : value))
    updateStock(index, { initialHolding: checked ? { heldShares: 0, coreShares: 0, targetShares: 0, averageCostPerShare: 0 } : null })
  }

  function updateHolding(index: number, key: "heldShares" | "coreShares" | "targetShares" | "averageCostPerShare", value: string) {
    const stock = stocks[index]
    if (!stock?.initialHolding) return
    updateStock(index, { initialHolding: { ...stock.initialHolding, [key]: Number(value) || 0 } })
  }

  function addStock() {
    setStocks((current) => [...current, newStock()])
    setWithHolding((current) => [...current, false])
  }

  function removeStock(index: number) {
    if (stocks.length === 1) return
    setStocks((current) => current.filter((_, itemIndex) => itemIndex !== index))
    setWithHolding((current) => current.filter((_, itemIndex) => itemIndex !== index))
  }

  async function submit() {
    setError(null)
    if (!portfolioName.trim() || stocks.some((stock) => !/^\d{6}$/.test(stock.securityCode.trim()))) {
      setError(copy.validationMessage)
      return
    }

    setSubmitting(true)
    try {
      const result = await initializeSetup({ portfolioName: portfolioName.trim(), stocks: stocks.map((stock) => ({ ...stock, securityCode: stock.securityCode.trim(), exchangeCode: stock.exchangeCode.trim().toUpperCase() })) })
      onComplete(result)
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, copy.requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="setup-frame">
      <header className="setup-topbar">
        <div className="setup-brand-lockup" aria-label={messages.common.ui.brandName}>
          <span className="setup-brand-mark" aria-hidden="true">D</span>
          <span className="setup-brand-copy">
            <strong>{messages.common.ui.brandName}</strong>
            <small>{messages.common.ui.brandCaption}</small>
          </span>
        </div>
        <div className="setup-topbar-state" role="status">
          <span className="setup-topbar-dot" aria-hidden="true" />
          <span><strong>{copy.eyebrow}</strong><small>{messages.common.ui.aShareOnly}</small></span>
        </div>
      </header>

      <main className="page-wrap setup-wrap">
        <div className="setup-page">
          <section className="setup-intro" aria-labelledby="setup-title">
            <div className="setup-intro-stamp" aria-hidden="true"><span>01</span><small>START</small></div>
            <p className="eyebrow">{copy.eyebrow}</p>
            <h1 id="setup-title" className="setup-title"><span>{copy.titleLineOne}</span><span>{copy.titleLineTwo}</span></h1>
            <p>{copy.description}</p>
            <Alert className="setup-callout" variant="attention">
              <span className="setup-callout-mark" aria-hidden="true">01</span>
              <div><AlertTitle>{copy.calloutTitle}</AlertTitle><AlertDescription>{copy.calloutDescription}</AlertDescription></div>
            </Alert>
          </section>

          <Card className="setup-card" aria-labelledby="setup-card-title">
            <CardHeader className="setup-card-heading">
              <div>
                <div className="setup-card-kicker"><span>01 / 03</span><i /></div>
                <p className="eyebrow">{copy.cardEyebrow}</p>
                <CardTitle id="setup-card-title">{copy.cardTitle}</CardTitle>
                <CardDescription>{copy.cardDescription}</CardDescription>
              </div>
              <span className="setup-card-mark" aria-hidden="true">D</span>
            </CardHeader>
            <Separator className="setup-card-divider" />
            {error && <Alert variant="destructive" className="setup-error"><AlertTitle>{copy.errorTitle}</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
            <CardContent className="setup-card-content">
              <form className="setup-form" onSubmit={(event) => { event.preventDefault(); void submit() }}>
                <FieldGroup className="setup-form-fields">
                  <Field className="setup-portfolio-field">
                    <FieldLabel htmlFor="portfolio-name">{copy.portfolioNameLabel}</FieldLabel>
                    <Input id="portfolio-name" value={portfolioName} onChange={(event) => setPortfolioName(event.target.value)} placeholder={copy.portfolioNamePlaceholder} required />
                  </Field>

                  <div className="setup-section-heading">
                    <div><span className="setup-section-number">01</span><div><strong>{copy.stockSectionTitle}</strong><small>{copy.stockSectionDescription}</small></div></div>
                    <span>{stocks.length}</span>
                  </div>

                  <FieldGroup className="setup-stock-list">
                    {stocks.map((stock, index) => (
                      <FieldGroup className="setup-stock-row" key={index}>
                        <div className="setup-stock-row-top"><span className="setup-stock-index">{String(index + 1).padStart(2, "0")}</span><strong>{copy.stockSectionTitle}</strong></div>
                        <FieldGroup className="setup-stock-fields">
                          <Field>
                            <FieldLabel htmlFor={`stock-code-${index}`}>{copy.stockCodeLabel}</FieldLabel>
                            <Input id={`stock-code-${index}`} value={stock.securityCode} maxLength={6} inputMode="numeric" onChange={(event) => updateStock(index, { securityCode: event.target.value.replace(/\D/g, "") })} placeholder={copy.stockCodePlaceholder} required />
                          </Field>
                          <Field>
                            <FieldLabel htmlFor={`exchange-${index}`}>{copy.exchangeLabel}</FieldLabel>
                            <Select value={stock.exchangeCode} onValueChange={(value) => { if (value) updateStock(index, { exchangeCode: value }) }}>
                              <SelectTrigger id={`exchange-${index}`} className="setup-select-trigger"><SelectValue>{stock.exchangeCode === "SSE" ? copy.exchangeSse : stock.exchangeCode === "SZSE" ? copy.exchangeSzse : copy.exchangeBse}</SelectValue></SelectTrigger>
                              <SelectContent><SelectGroup><SelectItem value="SSE">{copy.exchangeSse}</SelectItem><SelectItem value="SZSE">{copy.exchangeSzse}</SelectItem><SelectItem value="BSE">{copy.exchangeBse}</SelectItem></SelectGroup></SelectContent>
                            </Select>
                          </Field>
                          <Button className="remove-stock" size="icon" variant="ghost" type="button" aria-label={interpolate(copy.removeStock, { index: index + 1 })} disabled={stocks.length === 1} onClick={() => removeStock(index)}><Trash2 data-icon="inline-start" /></Button>
                        </FieldGroup>
                        <Field orientation="horizontal" className="holding-toggle">
                          <Checkbox id={`holding-${index}`} checked={withHolding[index] ?? false} onCheckedChange={(checked) => toggleHolding(index, checked === true)} />
                          <FieldLabel htmlFor={`holding-${index}`}>{copy.holdingToggle}</FieldLabel>
                        </Field>
                        {stock.initialHolding && <FieldGroup className="holding-fields">
                          <FieldDescription>{copy.holdingSectionDescription}</FieldDescription>
                          <Field><FieldLabel htmlFor={`held-shares-${index}`}>{copy.heldSharesLabel}</FieldLabel><Input id={`held-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.heldShares} onChange={(event) => updateHolding(index, "heldShares", event.target.value)} /></Field>
                          <Field><FieldLabel htmlFor={`core-shares-${index}`}>{copy.coreSharesLabel}</FieldLabel><Input id={`core-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.coreShares} onChange={(event) => updateHolding(index, "coreShares", event.target.value)} /></Field>
                          <Field><FieldLabel htmlFor={`target-shares-${index}`}>{copy.targetSharesLabel}</FieldLabel><Input id={`target-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.targetShares} onChange={(event) => updateHolding(index, "targetShares", event.target.value)} /></Field>
                          <Field><FieldLabel htmlFor={`average-cost-${index}`}>{copy.averageCostLabel}</FieldLabel><Input id={`average-cost-${index}`} type="number" min="0" step="0.01" value={stock.initialHolding.averageCostPerShare} onChange={(event) => updateHolding(index, "averageCostPerShare", event.target.value)} /></Field>
                        </FieldGroup>}
                      </FieldGroup>
                    ))}
                  </FieldGroup>

                  <Button className="setup-add-stock" variant="outline" type="button" onClick={addStock}><Plus data-icon="inline-start" />{copy.addStock}</Button>
                </FieldGroup>
                <div className="form-actions setup-submit-row"><Button size="lg" type="submit" disabled={submitting}>{submitting ? copy.submitting : copy.submit}<ArrowRight data-icon="inline-end" /></Button><span className="form-message">{copy.submitHint}</span></div>
              </form>
            </CardContent>
            <CardFooter className="setup-card-footer"><p className="setup-footnote">{copy.footnote}</p></CardFooter>
          </Card>
        </div>
      </main>

      <footer className="setup-footer"><span>{messages.common.ui.disclaimer}</span><span><i />{messages.common.ui.aShareOnly}</span></footer>
    </div>
  )
}
