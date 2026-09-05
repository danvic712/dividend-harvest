import { ArrowDownRight, ArrowUpRight, ShieldCheck } from "lucide-react"

import { interpolate, type OverviewCopy } from "@/lib/i18n"
import { currentPriceZoneLabel } from "@/lib/price-zones"
import { recommendationHeadline, recommendationSignalTitle, recommendationTone } from "@/lib/stock-display"
import type { StockAnalysisResult } from "@/lib/api-types"

type StockActionLabels = Pick<OverviewCopy["decision"], "actionLabel" | "headlines" | "signalTitles" | "priceZones" | "inZone">

export function StockActionSummary({ analysis, labels }: { analysis: StockAnalysisResult; labels: StockActionLabels }) {
  const tone = recommendationTone(analysis.recommendationCode)
  const headline = recommendationHeadline(analysis.recommendationCode, labels.headlines)
  const zone = currentPriceZoneLabel(analysis, labels.priceZones)
  const Icon = tone === "reduce" ? ArrowDownRight : tone === "hold" ? ShieldCheck : ArrowUpRight

  return (
    <section className={`stock-action-summary stock-action-summary-${tone}`} aria-live="polite" aria-label={labels.actionLabel}>
      <div className="stock-action-summary-mark" aria-hidden="true"><Icon size={18} /></div>
      <div className="stock-action-summary-copy">
        <span>{labels.actionLabel}</span>
        <h3>{headline.lead}<em>{headline.accent}</em></h3>
        <p>{recommendationSignalTitle(analysis.recommendationCode, labels.signalTitles)}</p>
      </div>
      <div className="stock-action-summary-zone">
        <span>{interpolate(labels.inZone, { zone })}</span>
        <strong>{zone}</strong>
      </div>
    </section>
  )
}
