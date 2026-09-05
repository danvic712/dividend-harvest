import { ArrowDownRight, ArrowRight, Check, Clock3, Coins, ShieldCheck, Target, WalletCards } from "lucide-react"

import { PriceLadder, PriceZoneBoard } from "@/components/price-ladder"
import { Button } from "@/components/ui/button"
import { interpolate, useLocale } from "@/lib/i18n"
import { currentPriceZoneLabel } from "@/lib/price-zones"
import { analysisDisplayName, recommendationHeadline, recommendationSignalTitle } from "@/lib/stock-display"
import { formatMoney, formatNumber, formatPercent } from "@/lib/utils"
import type { StockRecommendationResult } from "@/lib/api-types"

export function RecommendationDecision({ recommendation, onRecord }: { recommendation: StockRecommendationResult; onRecord: (direction: "buy" | "sell") => void }) {
  const { messages } = useLocale()
  const copy = messages.dividendStrategy.ui.overview
  const decisionCopy = copy.decision
  const { analysis } = recommendation
  const isSell = analysis.recommendationCode.toLowerCase().includes("trim")
  const actionShares = isSell ? recommendation.suggestedSellShares : recommendation.suggestedBuyShares
  const headline = recommendationHeadline(analysis.recommendationCode, decisionCopy.headlines)
  const currentZone = currentPriceZoneLabel(analysis, { strong_buy: decisionCopy.priceZones.strongBuy, accumulate: decisionCopy.priceZones.accumulate, hold: decisionCopy.priceZones.hold, partial_trim: decisionCopy.priceZones.partialTrim, aggressive_trim: decisionCopy.priceZones.aggressiveTrim })
  const securityName = analysisDisplayName(analysis)
  const actionLabel = actionShares > 0
    ? `${isSell ? decisionCopy.sell : decisionCopy.buy} ${formatNumber(actionShares, 0)} ${copy.ready.sharesUnit}`
    : decisionCopy.hold

  return (
    <>
      <section className="d-hero">
        <div className="d-story">
          <div className="d-kicker"><span>01</span><span>{decisionCopy.actionKicker}</span><i /></div>
          <div className="d-stock-identity">
            <div className="d-stock-avatar">{securityName.slice(0, 2)}</div>
            <div><strong>{securityName}</strong><span>{analysis.securityCode}.{analysis.exchangeCode} · {decisionCopy.stockReference}</span></div>
          </div>
          <h2 className="d-story-title">{headline.lead}<span>{headline.accent}</span></h2>
          <p className="d-hero-copy">{analysis.explanation || decisionCopy.fallbackExplanation}</p>
          <div className="d-action-row">
            <div className="d-action-amount">
              <div className="d-action-icon"><ArrowDownRight size={17} /></div>
              <div><span>{decisionCopy.actionLabel}</span><strong>{actionLabel}</strong><small>{actionShares > 0 ? interpolate(decisionCopy.amountAndZone, { amount: formatMoney(recommendation.suggestedTradeAmount), zone: currentZone }) : decisionCopy.waitingConfirmation}</small></div>
            </div>
            <Button className="d-primary-button" onClick={() => onRecord(actionShares > 0 ? (isSell ? "sell" : "buy") : "buy")}>
              {actionShares > 0 ? (isSell ? decisionCopy.recordSell : decisionCopy.recordBuy) : decisionCopy.recordHold}<ArrowRight data-icon="inline-end" />
            </Button>
          </div>
        </div>

        <section className="surface d-signal-card">
          <div className="d-card-topline"><span>02 / 03 · {decisionCopy.whyTitle}</span><Target size={16} /></div>
          <h2>{recommendationSignalTitle(analysis.recommendationCode, decisionCopy.signalTitles)}</h2>
          <p className="d-card-description">{interpolate(decisionCopy.lastTwelveMonthsFormula, { date: analysis.dataAsOfDate ?? decisionCopy.noDataDate })}</p>
          <div className="d-readout-grid">
            <div><span>{decisionCopy.latestPrice}</span><strong>{formatMoney(analysis.closePrice)}</strong><em>{analysis.priceZoneConfirmed ? decisionCopy.zoneConfirmed : decisionCopy.zonePending}</em></div>
            <div><span>{decisionCopy.actualYield}</span><strong>{formatPercent(analysis.dividendYield)}</strong><small>{interpolate(decisionCopy.dividendPerShare, { amount: formatMoney(analysis.modelDividendPerShare) })}</small></div>
          </div>
          <div className="d-reason-list">
            <div><span><Check size={12} /></span><p>{interpolate(decisionCopy.modelReason, { model: analysis.modelStatusCode, reliability: analysis.dividendReliabilityCode })}</p></div>
            <div><span><Check size={12} /></span><p>{interpolate(decisionCopy.zoneReason, { zone: currentZone })}</p></div>
            <div><span><Check size={12} /></span><p>{interpolate(decisionCopy.holdingReason, { held: formatNumber(analysis.heldShares, 0), core: formatNumber(analysis.coreShares, 0) })}</p></div>
          </div>
          <div className="d-data-foot"><Clock3 size={13} /> {decisionCopy.dataFrom}</div>
        </section>
      </section>

      <section className="d-ladder-section">
        <div className="d-section-head"><div><div className="d-kicker"><span>02</span><span>{decisionCopy.priceLadderKicker}</span><i /></div><h2>{decisionCopy.priceLadderTitle}</h2><p>{decisionCopy.priceLadderDescription}</p></div><div className="d-current-price"><span>{decisionCopy.latestPrice}</span><strong>{formatMoney(analysis.closePrice)}</strong><small>{interpolate(decisionCopy.inZone, { zone: currentZone })}</small></div></div>
        <div className="surface d-ladder-card">
          <PriceLadder analysis={analysis} />
          <PriceZoneBoard analysis={analysis} />
          <div className="d-ladder-caption"><span><b />{decisionCopy.ladderLegendMain}</span><span>{decisionCopy.ladderLegendSecondary}</span></div>
        </div>
      </section>

      <section className="d-plan-grid">
        <div className="surface d-plan-card">
          <div className="d-card-topline"><span>03 / 03 · {decisionCopy.executionPlan}</span><Coins size={16} /></div>
          <h2>{decisionCopy.planTitle}</h2>
          <p className="d-card-description">{interpolate(decisionCopy.planDescription, { securityName })}</p>
          <div className="d-plan-list">
            <div className="d-plan-row"><span className="d-plan-number d-plan-add">01</span><div><strong>{decisionCopy.currentRange}</strong><span>{currentZone}</span></div><div className="d-plan-action"><b>{actionLabel}</b><small>{formatMoney(recommendation.suggestedTradeAmount)}</small></div><Check size={15} /></div>
            <div className="d-plan-row"><span className="d-plan-number d-plan-strong">02</span><div><strong>{decisionCopy.nextStep}</strong><span>{decisionCopy.modelConfirming}</span></div><div className="d-plan-action"><b>{decisionCopy.continueLadder}</b><small>{decisionCopy.preserveCash}</small></div><Check size={15} /></div>
            <div className="d-plan-row"><span className="d-plan-number d-plan-hold">03</span><div><strong>{decisionCopy.protectionLine}</strong><span>{interpolate(decisionCopy.corePosition, { shares: formatNumber(analysis.coreShares, 0) })}</span></div><div className="d-plan-action"><b>{decisionCopy.noSwingTrading}</b><small>{decisionCopy.reevaluate}</small></div><Check size={15} /></div>
          </div>
          <div className="d-plan-note"><Coins size={15} /><span>{decisionCopy.planNote}</span></div>
        </div>

        <div className="surface d-position-card">
          <div className="d-card-topline"><span>{decisionCopy.yourPosition}</span><WalletCards size={16} /></div>
          <h2>{decisionCopy.positionTitle}</h2>
          <p className="d-card-description">{interpolate(decisionCopy.positionSummary, { securityName })}</p>
          <div className="d-position-hero"><strong>{formatNumber(analysis.heldShares, 0)}</strong><span> {decisionCopy.totalHolding}</span><b>{interpolate(decisionCopy.coreHolding, { shares: formatNumber(analysis.coreShares, 0) })}</b></div>
          <div className="d-position-meta"><div><span>{decisionCopy.coreShares}</span><strong>{formatNumber(analysis.coreShares, 0)} {copy.ready.sharesUnit}</strong></div><div><span>{decisionCopy.satelliteShares}</span><strong>{formatNumber(analysis.satelliteShares, 0)} {copy.ready.sharesUnit}</strong></div><div><span>{decisionCopy.zoneStatus}</span><strong>{analysis.priceZoneConfirmed ? decisionCopy.confirmed : decisionCopy.toConfirm}</strong></div></div>
        </div>
      </section>

      <section className="d-bottom-grid">
        <div className="surface d-dividend-card"><div className="d-bottom-heading"><div><span>{decisionCopy.cashFlowEvidence}</span><strong>{decisionCopy.actualDividend}</strong></div><div className="d-dividend-total"><b>{formatMoney(analysis.modelDividendPerShare)}</b><small>{interpolate(decisionCopy.perShareDataDate, { date: analysis.dataAsOfDate ?? "—" })}</small></div></div><div className="d-dividend-line"><div><span>{decisionCopy.dividendMode}</span><strong>{analysis.dividendModeCode ?? "—"}</strong><small>{decisionCopy.systemMethod}</small></div><div><span>{decisionCopy.reliability}</span><strong>{analysis.dividendReliabilityCode}</strong><small>{decisionCopy.noFillMissing}</small></div><div><span>{decisionCopy.calculationStatus}</span><strong>{analysis.modelStatusCode}</strong><small>{decisionCopy.fromAnalysis}</small></div></div></div>
        <div className="d-trust-note"><ShieldCheck size={20} /><div><strong>{decisionCopy.trustTitle}</strong><p>{decisionCopy.trustDescription}</p></div></div>
      </section>
    </>
  )
}
